import pygame, sys
import serial, serial.tools.list_ports
import time, random
from collections import deque
import statistics
import math

# Settings
BAUD_RATE     = 115200
SERIAL_PORT   = "auto"
TARGET_FPS    = 120

# Ball speed controls
BALL_START_SPEED = 4.0
BALL_MAX_SPEED   = 9.0
SPEEDUP_ON_HIT   = 1.06
MIN_BOUNCE_ANGLE = math.radians(12)
MAX_BOUNCE_ANGLE = math.radians(68)

# Smoothing
MEDIAN_WINDOW   = 7
EMA_ALPHA       = 0.35
MAX_STEP_PX     = 24
NO_DATA_HOLD_S  = 1.0

# PYGAME SETUP
pygame.init()
SCREEN_WIDTH, SCREEN_HEIGHT = 800, 600
screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
pygame.display.set_caption("Arduino Pong")

# Colors
BACKGROUND = (10, 12, 16)
LINE_GRAY  = (180, 185, 190)
BALL_COL   = (245, 246, 248)
LEFT_BLUE  = (88, 140, 210)
RIGHT_RED  = (214, 116, 116)

PADDLE_WIDTH, PADDLE_HEIGHT = 15, 100
BALL_RADIUS = 10

player_paddle = pygame.Rect(30, SCREEN_HEIGHT // 2 - PADDLE_HEIGHT // 2, PADDLE_WIDTH, PADDLE_HEIGHT)
cpu_paddle    = pygame.Rect(SCREEN_WIDTH - 30 - PADDLE_WIDTH, SCREEN_HEIGHT // 2 - PADDLE_HEIGHT // 2, PADDLE_WIDTH, PADDLE_HEIGHT)
ball          = pygame.Rect(SCREEN_WIDTH // 2 - BALL_RADIUS, SCREEN_HEIGHT // 2 - BALL_RADIUS, BALL_RADIUS * 2, BALL_RADIUS * 2)

# These will be set in reset_ball()
ball_speed_x = 0.0
ball_speed_y = 0.0
cpu_speed    = 6

player_score = 0
cpu_score    = 0
font  = pygame.font.Font(None, 74)
clock = pygame.time.Clock()

# ARDUINO SERIAL CONNECTION
def autodetect_port():
    for p in serial.tools.list_ports.comports():
        name = (p.device or "").lower()
        desc = (p.description or "").lower()
        if "usbmodem" in name or "usbserial" in name or "arduino" in desc or "ch340" in desc or "cp210" in desc:
            return p.device
    return None

arduino = None
arduino_connected = False
try:
    port = autodetect_port() if SERIAL_PORT == "auto" else SERIAL_PORT
    if not port:
        raise serial.SerialException("No Arduino-like serial port found (auto).")
    arduino = serial.Serial(port=port, baudrate=BAUD_RATE, timeout=0)
    time.sleep(2)
    arduino.reset_input_buffer()
    arduino_connected = True
    print(f"Connected to Arduino on {port}")
except Exception as e:
    print("Could not connect to Arduino. Falling back to keyboard (Up/Down).")
    print(f"Reason: {e}")

# Helpers
def clamp_ball_speed(vx, vy, max_speed=BALL_MAX_SPEED):
    mag = math.hypot(vx, vy)
    if mag > max_speed and mag > 0:
        scale = max_speed / mag
        return vx * scale, vy * scale
    return vx, vy

def launch_vector(speed=BALL_START_SPEED):
    # Choose a random angle within safe bounds; randomize left/right
    sign_x = random.choice((-1, 1))
    angle  = random.uniform(MIN_BOUNCE_ANGLE, MAX_BOUNCE_ANGLE)
    angle *= random.choice((-1, 1))  # up or down
    vx = sign_x * speed * math.cos(angle)
    vy = speed * math.sin(angle)
    return vx, vy

def reset_ball():
    global ball_speed_x, ball_speed_y
    ball.center = (SCREEN_WIDTH // 2, SCREEN_HEIGHT // 2)
    ball_speed_x, ball_speed_y = launch_vector(BALL_START_SPEED)

def draw_elements():
    screen.fill(BACKGROUND)
    # center line
    pygame.draw.aaline(screen, LINE_GRAY, (SCREEN_WIDTH // 2, 0), (SCREEN_WIDTH // 2, SCREEN_HEIGHT))
    # paddles
    pygame.draw.rect(screen, LEFT_BLUE,  player_paddle, border_radius=4)
    pygame.draw.rect(screen, RIGHT_RED,  cpu_paddle,    border_radius=4)
    # ball
    pygame.draw.ellipse(screen, BALL_COL, ball)
    # scores
    player_text = font.render(str(player_score), True, LEFT_BLUE)
    cpu_text    = font.render(str(cpu_score), True, RIGHT_RED)
    screen.blit(player_text, (SCREEN_WIDTH // 4, 10))
    screen.blit(cpu_text, (SCREEN_WIDTH * 3 // 4 - cpu_text.get_width(), 10))

# Input filters state
pot_history   = deque(maxlen=MEDIAN_WINDOW)
last_pot_time = time.monotonic()
last_valid_pot = 512  # start centered
ema_y = float(player_paddle.y)

def map_pot_to_y(pot):
    # 0 -> bottom, 1023 -> top
    pot = max(0, min(1023, pot))
    return (1.0 - (pot / 1023.0)) * (SCREEN_HEIGHT - PADDLE_HEIGHT)


# Start game
reset_ball()

# --- MAIN GAME LOOP ---
while True:
    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            if arduino_connected:
                try: arduino.close()
                except: pass
            pygame.quit()
            sys.exit()

    # --- INPUT HANDLING ---
    fresh_value = False
    newest_line = None

    if arduino_connected:
        try:
            # Drain buffer; keep only newest complete line
            while arduino.in_waiting:
                raw = arduino.readline()  # immediate (timeout=0)
                if raw.endswith(b'\n'):
                    newest_line = raw

            if newest_line:
                s = newest_line.decode('utf-8', errors='ignore').strip()
                # accept plain integer lines only
                if s.isdigit():
                    pot = int(s)
                    if 0 <= pot <= 1023:
                        pot_history.append(pot)
                        last_valid_pot = pot
                        last_pot_time  = time.monotonic()
                        fresh_value = True
        except Exception:
            pass

        # If no fresh value recently, hold last position (do NOT snap to 0)
        now = time.monotonic()
        if not fresh_value and (now - last_pot_time) > NO_DATA_HOLD_S:
            pass

        # Use robust estimate (median) to kill spikes (like single-frame zeros)
        robust_pot = int(statistics.median(pot_history)) if pot_history else last_valid_pot
        target_y = map_pot_to_y(robust_pot)

    else:
        # Keyboard fallback
        keys = pygame.key.get_pressed()
        if keys[pygame.K_UP]:   player_paddle.y -= 7
        if keys[pygame.K_DOWN]: player_paddle.y += 7
        target_y = float(player_paddle.y)

    # --- Apply smoothing & slew limit ---
    ema_y = EMA_ALPHA * float(target_y) + (1 - EMA_ALPHA) * ema_y
    dy = ema_y - player_paddle.y
    if dy >  MAX_STEP_PX: dy =  MAX_STEP_PX
    if dy < -MAX_STEP_PX: dy = -MAX_STEP_PX
    player_paddle.y += dy

    # Keep paddles on screen
    if player_paddle.top < 0: player_paddle.top = 0
    if player_paddle.bottom > SCREEN_HEIGHT: player_paddle.bottom = SCREEN_HEIGHT

    # CPU AI
    if cpu_paddle.centery < ball.centery:   cpu_paddle.y += cpu_speed
    elif cpu_paddle.centery > ball.centery: cpu_paddle.y -= cpu_speed
    if cpu_paddle.top < 0: cpu_paddle.top = 0
    if cpu_paddle.bottom > SCREEN_HEIGHT: cpu_paddle.bottom = SCREEN_HEIGHT

    # --- GAME LOGIC ---
    # move ball
    ball.x += ball_speed_x
    ball.y += ball_speed_y

    # top/bottom collision
    if ball.top <= 0 or ball.bottom >= SCREEN_HEIGHT:
        ball_speed_y *= -1

    # paddle collision + controlled speed-up
    hit = False
    # Slightly adjust the outgoing angle based on impact position (adds skill)
    def add_aim_offset(rect):
        # relative hit position (-0.5 .. 0.5)
        rel = ((ball.centery - rect.centery) / (rect.height / 2.0))
        rel = max(-1.0, min(1.0, rel))
        return rel * math.radians(20)  # at most +/-20 degrees of aim

    if ball.colliderect(player_paddle):
        # ensure ball goes right
        ball.left = player_paddle.right
        hit = True
        aim = add_aim_offset(player_paddle)
        # reflect
        angle = math.atan2(ball_speed_y, abs(ball_speed_x))
        angle += aim
        # clamp angle to safe range
        angle = max(-MAX_BOUNCE_ANGLE, min(MAX_BOUNCE_ANGLE, angle))
        # speed-up and rebuild vector pointing right
        speed = min(math.hypot(ball_speed_x, ball_speed_y) * SPEEDUP_ON_HIT, BALL_MAX_SPEED)
        ball_speed_x =  abs(speed * math.cos(angle))
        ball_speed_y =      speed * math.sin(angle)

    elif ball.colliderect(cpu_paddle):
        # ensure ball goes left
        ball.right = cpu_paddle.left
        hit = True
        aim = add_aim_offset(cpu_paddle)
        angle = math.atan2(ball_speed_y, -abs(ball_speed_x))
        angle += aim
        angle = max(-MAX_BOUNCE_ANGLE, min(MAX_BOUNCE_ANGLE, angle))
        speed = min(math.hypot(ball_speed_x, ball_speed_y) * SPEEDUP_ON_HIT, BALL_MAX_SPEED)
        ball_speed_x = -abs(speed * math.cos(angle))
        ball_speed_y =      speed * math.sin(angle)

    if hit:
        # keep angle away from near-flat horizontals
        # if too flat, nudge vy slightly
        if abs(ball_speed_y) < math.sin(MIN_BOUNCE_ANGLE) * abs(math.hypot(ball_speed_x, ball_speed_y)):
            ball_speed_y = math.copysign(
                math.sin(MIN_BOUNCE_ANGLE) * abs(math.hypot(ball_speed_x, ball_speed_y)),
                ball_speed_y if ball_speed_y != 0 else random.choice((-1, 1))
            )
        # final safety clamp
        ball_speed_x, ball_speed_y = clamp_ball_speed(ball_speed_x, ball_speed_y)

    # Scoring
    if ball.left <= 0:
        cpu_score += 1
        reset_ball()
    if ball.right >= SCREEN_WIDTH:
        player_score += 1
        reset_ball()

    # --- DRAW ---
    draw_elements()
    pygame.display.flip()
    clock.tick(TARGET_FPS)

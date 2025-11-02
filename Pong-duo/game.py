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
DEBUG_HUD     = True 

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

# Pygame setup 
pygame.init()
SCREEN_WIDTH, SCREEN_HEIGHT = 800, 600
screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
pygame.display.set_caption("Arduino Pong - 2 Players")

# Colors
BACKGROUND = (10, 12, 16)
LINE_GRAY  = (180, 185, 190)
BALL_COL   = (245, 246, 248)
LEFT_BLUE  = (88, 140, 210)   # A0
RIGHT_RED  = (214, 116, 116)  # A1
HUD_GRAY   = (200, 205, 210)

PADDLE_WIDTH, PADDLE_HEIGHT = 15, 100
BALL_RADIUS = 10

left_paddle  = pygame.Rect(30, SCREEN_HEIGHT // 2 - PADDLE_HEIGHT // 2,  PADDLE_WIDTH, PADDLE_HEIGHT)
right_paddle = pygame.Rect(SCREEN_WIDTH - 30 - PADDLE_WIDTH, SCREEN_HEIGHT // 2 - PADDLE_HEIGHT // 2, PADDLE_WIDTH, PADDLE_HEIGHT)
ball         = pygame.Rect(SCREEN_WIDTH // 2 - BALL_RADIUS, SCREEN_HEIGHT // 2 - BALL_RADIUS, BALL_RADIUS * 2, BALL_RADIUS * 2)

ball_speed_x = 0.0
ball_speed_y = 0.0

score_left  = 0
score_right = 0
font  = pygame.font.Font(None, 74)
font_hud = pygame.font.Font(None, 26)
clock = pygame.time.Clock()

# ---------------- Serial ----------------
def autodetect_port():
    for p in serial.tools.list_ports.comports():
        name = (p.device or "").lower()
        desc = (p.description or "").lower()
        if "usbmodem" in name or "usbserial" in name or "arduino" in desc or "ch340" in desc or "cp210" in desc:
            return p.device
    return None

try:
    port = autodetect_port() if SERIAL_PORT == "auto" else SERIAL_PORT
    if not port:
        raise serial.SerialException("No Arduino-like serial port found (auto).")
    ser = serial.Serial(port=port, baudrate=BAUD_RATE, timeout=0)  # non-blocking
    time.sleep(2)
    ser.reset_input_buffer()
    print(f"Connected to Arduino on {port}")
except Exception as e:
    print("❌ Could not connect to Arduino. Close Serial Monitor and set SERIAL_PORT if needed.")
    print("Reason:", e)
    pygame.quit(); sys.exit(1)

# We'll accumulate bytes here and split on newlines
serial_buf = bytearray()
last_line  = ""
last_v0    = 512
last_v1    = 512

# ---------------- Helpers ----------------
def clamp_ball_speed(vx, vy, max_speed=BALL_MAX_SPEED):
    mag = math.hypot(vx, vy)
    if mag > max_speed and mag > 0:
        s = max_speed / mag
        return vx * s, vy * s
    return vx, vy

def launch_vector(speed=BALL_START_SPEED):
    sign_x = random.choice((-1, 1))
    angle  = random.uniform(MIN_BOUNCE_ANGLE, MAX_BOUNCE_ANGLE)
    angle *= random.choice((-1, 1))  # up or down
    return sign_x * speed * math.cos(angle), speed * math.sin(angle)

def reset_ball():
    global ball_speed_x, ball_speed_y
    ball.center = (SCREEN_WIDTH // 2, SCREEN_HEIGHT // 2)
    ball_speed_x, ball_speed_y = launch_vector(BALL_START_SPEED)

def draw_elements():
    screen.fill(BACKGROUND)
    pygame.draw.aaline(screen, LINE_GRAY, (SCREEN_WIDTH // 2, 0), (SCREEN_WIDTH // 2, SCREEN_HEIGHT))
    pygame.draw.rect(screen, LEFT_BLUE,  left_paddle,  border_radius=4)
    pygame.draw.rect(screen, RIGHT_RED,  right_paddle, border_radius=4)
    pygame.draw.ellipse(screen, BALL_COL, ball)
    # scores
    ltxt = font.render(str(score_left),  True, LEFT_BLUE)
    rtxt = font.render(str(score_right), True, RIGHT_RED)
    screen.blit(ltxt, (SCREEN_WIDTH // 4, 10))
    screen.blit(rtxt, (SCREEN_WIDTH * 3 // 4 - rtxt.get_width(), 10))
    # HUD
    if DEBUG_HUD:
        hud1 = font_hud.render(f"Serial: {last_line}", True, HUD_GRAY)
        hud2 = font_hud.render(f"A0={last_v0}  A1={last_v1}", True, HUD_GRAY)
        screen.blit(hud1, (10, SCREEN_HEIGHT - 50))
        screen.blit(hud2, (10, SCREEN_HEIGHT - 25))

def map_pot_to_y(pot):
    # 0 -> bottom, 1023 -> top
    pot = max(0, min(1023, pot))
    return (1.0 - (pot / 1023.0)) * (SCREEN_HEIGHT - PADDLE_HEIGHT)

# Input filters state (per paddle)
hist_L = deque(maxlen=MEDIAN_WINDOW)
hist_R = deque(maxlen=MEDIAN_WINDOW)
last_L = 512
last_R = 512
t_L = time.monotonic()
t_R = time.monotonic()
ema_L = float(left_paddle.y)
ema_R = float(right_paddle.y)

# Start game
reset_ball()

# ---------------- Main loop ----------------
while True:
    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            try: ser.close()
            except: pass
            pygame.quit(); sys.exit()
        elif event.type == pygame.KEYDOWN:
            if event.key == pygame.K_F1:
                DEBUG_HUD = not DEBUG_HUD
            elif event.key == pygame.K_r:
                ser.reset_input_buffer()
                serial_buf.clear()
                print("🔄 Serial input buffer flushed.")

    # --- Robust line reader: read whatever is available, split on '\n' ---
    try:
        chunk = ser.read(ser.in_waiting or 1)   # grab bytes without blocking
        if chunk:
            serial_buf.extend(chunk)
            # Split into complete lines; keep any trailing partial
            *lines, remainder = serial_buf.split(b'\n')
            serial_buf = bytearray(remainder)   # save partial for next loop

            if lines:
                raw = lines[-1].rstrip(b'\r')   # use the newest complete line
                s = raw.decode('ascii', errors='ignore').strip()
                last_line = s  # for HUD/console

                # Expect "v0,v1" (strict numbers separated by comma)
                if ',' in s:
                    try:
                        a, b = s.split(',', 1)
                        v0 = int(a.strip()); v1 = int(b.strip())
                        last_v0, last_v1 = v0, v1  # HUD
                        if 0 <= v0 <= 1023:
                            hist_L.append(v0); last_L = v0; t_L = time.monotonic()
                        if 0 <= v1 <= 1023:
                            hist_R.append(v1); last_R = v1; t_R = time.monotonic()
                    except ValueError:
                        pass
    except Exception as e:
        # Print once in a while if you want: print("Serial read error:", e)
        pass

    # Hold last good if stream pauses
    now = time.monotonic()
    robust_L = int(statistics.median(hist_L)) if hist_L else last_L
    robust_R = int(statistics.median(hist_R)) if hist_R else last_R
    if (now - t_L) > NO_DATA_HOLD_S: robust_L = last_L
    if (now - t_R) > NO_DATA_HOLD_S: robust_R = last_R

    # Map -> EMA -> slew (left = A0, right = A1)
    target_L = map_pot_to_y(robust_L)
    target_R = map_pot_to_y(robust_R)

    ema_L = EMA_ALPHA * target_L + (1 - EMA_ALPHA) * ema_L
    ema_R = EMA_ALPHA * target_R + (1 - EMA_ALPHA) * ema_R

    dL = ema_L - left_paddle.y
    dR = ema_R - right_paddle.y
    dL = max(-MAX_STEP_PX, min(MAX_STEP_PX, dL))
    dR = max(-MAX_STEP_PX, min(MAX_STEP_PX, dR))
    left_paddle.y  += dL
    right_paddle.y += dR

    # Constrain paddles
    left_paddle.top  = max(0, min(left_paddle.top,  SCREEN_HEIGHT - PADDLE_HEIGHT))
    right_paddle.top = max(0, min(right_paddle.top, SCREEN_HEIGHT - PADDLE_HEIGHT))

    # --- Ball physics ---
    ball.x += ball_speed_x
    ball.y += ball_speed_y

    # top/bottom bounce
    if ball.top <= 0 or ball.bottom >= SCREEN_HEIGHT:
        ball_speed_y *= -1

    # Aim helper based on hit position
    def aim_offset(rect):
        rel = (ball.centery - rect.centery) / (rect.height / 2.0)  # -1..1
        rel = max(-1.0, min(1.0, rel))
        return rel * math.radians(20)

    hit = False
    # Left paddle (ball must be moving left)
    if ball.colliderect(left_paddle) and ball_speed_x < 0:
        ball.left = left_paddle.right
        hit = True
        angle = math.atan2(ball_speed_y,  abs(ball_speed_x))
        angle += aim_offset(left_paddle)
        angle = max(-MAX_BOUNCE_ANGLE, min(MAX_BOUNCE_ANGLE, angle))
        speed = min(math.hypot(ball_speed_x, ball_speed_y) * SPEEDUP_ON_HIT, BALL_MAX_SPEED)
        ball_speed_x =  abs(speed * math.cos(angle))
        ball_speed_y =      speed * math.sin(angle)

    # Right paddle (ball must be moving right)
    if ball.colliderect(right_paddle) and ball_speed_x > 0:
        ball.right = right_paddle.left
        hit = True
        angle = math.atan2(ball_speed_y, -abs(ball_speed_x))
        angle += aim_offset(right_paddle)
        angle = max(-MAX_BOUNCE_ANGLE, min(MAX_BOUNCE_ANGLE, angle))
        speed = min(math.hypot(ball_speed_x, ball_speed_y) * SPEEDUP_ON_HIT, BALL_MAX_SPEED)
        ball_speed_x = -abs(speed * math.cos(angle))
        ball_speed_y =      speed * math.sin(angle)

    if hit:
        mag = math.hypot(ball_speed_x, ball_speed_y)
        min_vy = math.sin(MIN_BOUNCE_ANGLE) * mag
        if abs(ball_speed_y) < min_vy:
            ball_speed_y = math.copysign(min_vy, ball_speed_y if ball_speed_y != 0 else random.choice((-1, 1)))
        ball_speed_x, ball_speed_y = clamp_ball_speed(ball_speed_x, ball_speed_y)

    # Scoring
    if ball.left <= 0:
        score_right += 1
        reset_ball()
    if ball.right >= SCREEN_WIDTH:
        score_left  += 1
        reset_ball()

    # Draw
    draw_elements()
    pygame.display.flip()
    clock.tick(TARGET_FPS)

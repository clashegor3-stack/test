import random
import threading
import time
import tkinter as tk
from tkinter import ttk, messagebox

import pyautogui
from pynput import keyboard

pyautogui.FAILSAFE = True

class AntiAFKApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Russia Online Anti-AFK")
        self.root.geometry("360x260")
        self.root.resizable(False, False)

        self.running = threading.Event()
        self.shutdown = threading.Event()
        self.worker = None
        self.listener = None

        self.key_var = tk.StringVar(value="w")
        self.min_var = tk.IntVar(value=45)
        self.max_var = tk.IntVar(value=65)
        self.duration_var = tk.DoubleVar(value=0.2)
        self.status_var = tk.StringVar(value="Выключен")

        frame = ttk.Frame(root, padding=14)
        frame.pack(fill="both", expand=True)

        ttk.Label(frame, text="Клавиша для короткого нажатия:").pack(anchor="w")
        ttk.Entry(frame, textvariable=self.key_var, width=12).pack(anchor="w", pady=(2, 8))

        row = ttk.Frame(frame)
        row.pack(fill="x")
        ttk.Label(row, text="Интервал от (сек.):").grid(row=0, column=0, sticky="w")
        ttk.Entry(row, textvariable=self.min_var, width=7).grid(row=0, column=1, padx=6)
        ttk.Label(row, text="до:").grid(row=0, column=2)
        ttk.Entry(row, textvariable=self.max_var, width=7).grid(row=0, column=3, padx=6)

        ttk.Label(frame, text="Длительность нажатия (сек.):").pack(anchor="w", pady=(8, 0))
        ttk.Entry(frame, textvariable=self.duration_var, width=10).pack(anchor="w", pady=(2, 8))

        buttons = ttk.Frame(frame)
        buttons.pack(fill="x", pady=6)
        self.toggle_btn = ttk.Button(buttons, text="Включить (F6)", command=self.toggle)
        self.toggle_btn.pack(side="left", expand=True, fill="x", padx=(0, 4))
        ttk.Button(buttons, text="Выход (F7)", command=self.close).pack(side="left", expand=True, fill="x", padx=(4, 0))

        ttk.Label(frame, text="Статус:").pack(anchor="w", pady=(8, 0))
        ttk.Label(frame, textvariable=self.status_var, font=("Segoe UI", 11, "bold")).pack(anchor="w")
        ttk.Label(frame, text="Переключение: F6   |   Выход: F7", foreground="gray").pack(anchor="w", pady=(8, 0))

        self.worker = threading.Thread(target=self.loop, daemon=True)
        self.worker.start()
        self.listener = keyboard.GlobalHotKeys({
            "<f6>": self.hotkey_toggle,
            "<f7>": self.hotkey_close
        })
        self.listener.start()
        self.root.protocol("WM_DELETE_WINDOW", self.close)

    def hotkey_toggle(self):
        self.root.after(0, self.toggle)

    def hotkey_close(self):
        self.root.after(0, self.close)

    def toggle(self):
        if self.running.is_set():
            self.running.clear()
            self.status_var.set("Выключен")
            self.toggle_btn.config(text="Включить (F6)")
        else:
            try:
                key = self.key_var.get().strip().lower()
                low, high = int(self.min_var.get()), int(self.max_var.get())
                duration = float(self.duration_var.get())
                if not key or len(key) > 1:
                    raise ValueError("Укажи одну клавишу, например w или space.")
                if low < 1 or high < low:
                    raise ValueError("Интервал должен быть положительным, а верхняя граница не меньше нижней.")
                if not 0.05 <= duration <= 2:
                    raise ValueError("Длительность должна быть от 0.05 до 2 секунд.")
            except Exception as exc:
                messagebox.showerror("Проверь настройки", str(exc))
                return
            self.running.set()
            self.status_var.set("Работает")
            self.toggle_btn.config(text="Выключить (F6)")

    def loop(self):
        while not self.shutdown.is_set():
            if self.running.wait(timeout=0.2):
                try:
                    key = self.key_var.get().strip().lower()
                    duration = float(self.duration_var.get())
                    pyautogui.keyDown(key)
                    time.sleep(duration)
                    pyautogui.keyUp(key)
                    low, high = int(self.min_var.get()), int(self.max_var.get())
                    delay = random.uniform(low, high)
                except Exception:
                    try:
                        pyautogui.keyUp(self.key_var.get().strip().lower())
                    except Exception:
                        pass
                    self.running.clear()
                    self.root.after(0, lambda: self.status_var.set("Ошибка, выключен"))
                    self.root.after(0, lambda: self.toggle_btn.config(text="Включить (F6)"))
                    time.sleep(0.5)
                    continue

                end = time.monotonic() + delay
                while self.running.is_set() and not self.shutdown.is_set() and time.monotonic() < end:
                    time.sleep(0.1)

    def close(self):
        self.running.clear()
        self.shutdown.set()
        try:
            if self.listener:
                self.listener.stop()
        except Exception:
            pass
        self.root.destroy()

if __name__ == "__main__":
    root = tk.Tk()
    app = AntiAFKApp(root)
    root.mainloop()

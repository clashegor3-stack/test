using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_F6 = 1, HOTKEY_F7 = 2;
    private const uint VK_F6 = 0x75, VK_F7 = 0x76;
    private const uint INPUT_KEYBOARD = 1, KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    private readonly Label status = new();
    private readonly Label lamp = new();
    private readonly Button startButton = new();
    private readonly Button stopButton = new();
    private readonly TextBox keyBox = new();
    private readonly NumericUpDown minBox = new();
    private readonly NumericUpDown maxBox = new();
    private readonly NumericUpDown pressBox = new();
    private readonly System.Windows.Forms.Timer cycleTimer = new();
    private readonly System.Windows.Forms.Timer releaseTimer = new();
    private bool active;
    private bool keyIsDown;
    private Keys currentKey = Keys.W;
    private readonly Random random = new();

    public MainForm()
    {
        Text = "Russia Online | Anti-AFK";
        ClientSize = new Size(430, 390);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(17, 24, 39);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        var title = new Label { Text = "RUSSIA ONLINE", Font = new Font("Segoe UI", 19, FontStyle.Bold),
            ForeColor = Color.White, AutoSize = true, Location = new Point(22, 18) };
        var subtitle = new Label { Text = "ANTI-AFK CONTROL PANEL", ForeColor = Color.FromArgb(96, 165, 250),
            Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Location = new Point(24, 55) };
        Controls.Add(title); Controls.Add(subtitle);

        var card = new Panel { BackColor = Color.FromArgb(31, 41, 55), Location = new Point(22, 88), Size = new Size(386, 78) };
        lamp.Text = "●"; lamp.Font = new Font("Segoe UI", 20, FontStyle.Bold); lamp.ForeColor = Color.FromArgb(239, 68, 68);
        lamp.AutoSize = true; lamp.Location = new Point(14, 14);
        status.Text = "ОСТАНОВЛЕНА"; status.Font = new Font("Segoe UI", 13, FontStyle.Bold);
        status.ForeColor = Color.White; status.AutoSize = true; status.Location = new Point(48, 24);
        card.Controls.Add(lamp); card.Controls.Add(status); Controls.Add(card);

        AddLabel("Клавиша (W/A/S/D)", 22, 184);
        keyBox.Text = "W"; keyBox.CharacterCasing = CharacterCasing.Upper;
        keyBox.MaxLength = 1; StyleInput(keyBox); keyBox.Location = new Point(22, 207); keyBox.Width = 75;
        Controls.Add(keyBox);

        AddLabel("Интервал от (сек.)", 120, 184);
        ConfigureNumeric(minBox, 45, 1, 3600, 120, 207);
        AddLabel("Интервал до (сек.)", 225, 184);
        ConfigureNumeric(maxBox, 65, 1, 3600, 225, 207);
        AddLabel("Нажатие (мс.)", 330, 184);
        ConfigureNumeric(pressBox, 200, 50, 2000, 330, 207);

        startButton.Text = "▶  ЗАПУСТИТЬ"; startButton.Location = new Point(22, 260);
        startButton.Size = new Size(185, 48); startButton.FlatStyle = FlatStyle.Flat;
        startButton.FlatAppearance.BorderSize = 0; startButton.BackColor = Color.FromArgb(22, 163, 74);
        startButton.ForeColor = Color.White; startButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        startButton.Click += (_, _) => StartAntiAfk();
        Controls.Add(startButton);

        stopButton.Text = "■  ОСТАНОВИТЬ"; stopButton.Location = new Point(223, 260);
        stopButton.Size = new Size(185, 48); stopButton.FlatStyle = FlatStyle.Flat;
        stopButton.FlatAppearance.BorderSize = 0; stopButton.BackColor = Color.FromArgb(55, 65, 81);
        stopButton.ForeColor = Color.White; stopButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        stopButton.Enabled = false; stopButton.Click += (_, _) => StopAntiAfk();
        Controls.Add(stopButton);

        var help = new Label { Text = "F6 — включить/выключить    •    F7 — выход",
            ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true, Location = new Point(22, 330) };
        var warning = new Label { Text = "Проверь правила сервера: макросы могут быть запрещены.",
            ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true, Location = new Point(22, 353) };
        Controls.Add(help); Controls.Add(warning);

        cycleTimer.Tick += (_, _) => PressConfiguredKey();
        releaseTimer.Tick += (_, _) => ReleaseKey();

        Shown += (_, _) =>
        {
            RegisterHotKey(Handle, HOTKEY_F6, 0, VK_F6);
            RegisterHotKey(Handle, HOTKEY_F7, 0, VK_F7);
        };
        FormClosing += (_, _) => { StopAntiAfk(); UnregisterHotKey(Handle, HOTKEY_F6); UnregisterHotKey(Handle, HOTKEY_F7); };
    }

    private void AddLabel(string text, int x, int y) =>
        Controls.Add(new Label { Text = text, ForeColor = Color.FromArgb(209, 213, 219), AutoSize = true, Location = new Point(x, y) });

    private static void StyleInput(TextBox box)
    {
        box.BackColor = Color.FromArgb(31, 41, 55); box.ForeColor = Color.White;
        box.BorderStyle = BorderStyle.FixedSingle; box.Font = new Font("Segoe UI", 10);
    }

    private void ConfigureNumeric(NumericUpDown box, decimal value, decimal min, decimal max, int x, int y)
    {
        box.Minimum = min; box.Maximum = max; box.Value = value; box.Location = new Point(x, y);
        box.Width = 85; box.BackColor = Color.FromArgb(31, 41, 55); box.ForeColor = Color.White;
        box.BorderStyle = BorderStyle.FixedSingle; box.Font = new Font("Segoe UI", 10);
        Controls.Add(box);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            if (m.WParam.ToInt32() == HOTKEY_F6)
            {
                if (active) StopAntiAfk(); else StartAntiAfk();
            }
            else if (m.WParam.ToInt32() == HOTKEY_F7) Close();
        }
        base.WndProc(ref m);
    }

    private void StartAntiAfk()
    {
        if (!Enum.TryParse(keyBox.Text.Trim(), true, out Keys parsed) ||
            parsed is not (Keys.W or Keys.A or Keys.S or Keys.D))
        {
            MessageBox.Show("Укажи одну из клавиш W, A, S или D.", "Проверь настройки",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (maxBox.Value < minBox.Value)
        {
            MessageBox.Show("Интервал «до» не может быть меньше интервала «от».",
                "Проверь настройки", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        currentKey = parsed;
        active = true;
        status.Text = "ЗАПУЩЕНА"; lamp.ForeColor = Color.FromArgb(34, 197, 94);
        startButton.Enabled = false; stopButton.Enabled = true;
        ScheduleNext();
    }

    private void StopAntiAfk()
    {
        active = false;
        cycleTimer.Stop(); releaseTimer.Stop();
        ReleaseKey();
        status.Text = "ОСТАНОВЛЕНА"; lamp.ForeColor = Color.FromArgb(239, 68, 68);
        startButton.Enabled = true; stopButton.Enabled = false;
    }

    private void ScheduleNext()
    {
        if (!active) return;
        int min = (int)minBox.Value, max = (int)maxBox.Value;
        cycleTimer.Interval = random.Next(min * 1000, checked(max * 1000 + 1));
        cycleTimer.Start();
    }

    private void PressConfiguredKey()
    {
        cycleTimer.Stop();
        if (!active) return;
        SendKey(currentKey, false); keyIsDown = true;
        releaseTimer.Interval = (int)pressBox.Value;
        releaseTimer.Start();
    }

    private void ReleaseKey()
    {
        releaseTimer.Stop();
        if (keyIsDown) { SendKey(currentKey, true); keyIsDown = false; }
        if (active) ScheduleNext();
    }

    private static void SendKey(Keys key, bool keyUp)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion { ki = new KEYBDINPUT
            {
                wVk = (ushort)key, wScan = 0,
                dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                time = 0, dwExtraInfo = IntPtr.Zero
            }}
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }
}

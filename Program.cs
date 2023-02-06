using JyunrcaeaFramework;
using System.Data;
using System.Management;
using Windows.System;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Microsoft.VisualBasic;
using System.Diagnostics;
using Windows.ApplicationModel.Wallet;
using System.Runtime.CompilerServices;
using Windows.UI.Notifications;

class Program
{

    public static Clock cc;
    public static MainScene ms;
    public static Welcome wc;
    public static curt ct;
    
    static void Main(string[] args)
    {
        if (!Directory.Exists("cache"))
        {
            DirectoryInfo info = Directory.CreateDirectory("cache");    
            info.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
        }
        if (!File.Exists("cache\\font.ttf"))
        {
            File.WriteAllBytes("cache\\font.ttf", AlwaysOnDisplay.Properties.Resources.font);
        }
        Framework.MultiCoreProcess = true;
        Framework.BackgroundColor = new(0, 0, 0);
        Framework.Init("Always On Display", 720, 480, null, null, new());
        Display.AddScene(wc = new Welcome());
        Task.Run(() =>
        {
            Display.AddScene(ms = new MainScene());
            Display.AddScene(cc = new Clock());
            Display.AddScene(new WindowState());
            Display.AddScene(ct = new curt());
            wc.Finish();
        });
        //절반 제한
        Display.FrameLateLimit = Display.FrameLateLimit / 2;
        Framework.Run();
    }

    public static BatteryStatusInfo GetBattery()
    {
        BatteryStatusInfo info = new();

        ManagementObjectCollection collection = (new ManagementObjectSearcher(new ObjectQuery("Select * FROM Win32_Battery"))).Get();
        foreach (ManagementObject mo in collection)
        {
            foreach (PropertyData property in mo.Properties)
            {
                switch(property.Name)
                {
                    case "BatteryStatus":
                        info.bs = (UInt16)property.Value;
                        break;
                    case "EstimatedChargeRemaining":
                        info.ecr = (UInt16)property.Value;
                        break;
                    case "EstimatedRunTime":
                        info.ert = (UInt32)property.Value;
                        break;
                }
                //Console.WriteLine("Property {0}: Value is {1}", property.Name, property.Value);
            }
        }

        return info;
    }
}

class BatteryStatusInfo
{
    public UInt16 ecr,bs;
    public UInt32 ert;

    public BatteryStatusInfo(UInt16 pct,uint runtime, UInt16 betterystatus) {
        ecr = pct;
        ert = runtime;
        bs = betterystatus;
    }

    public BatteryStatusInfo() { }
}

class DateText : TextboxForAnimation
{
    public DateText() : base("cache/font.ttf", 20, "날짜 검색중") { this.Y = 40; this.DrawY = VerticalPositionType.Bottom; }

    float uptime = 0;

    public override void Update(float ms)
    {
        if (uptime < Framework.RunningTime)
        {
            var t = DateTime.Now;
            uptime += (new DateTime(t.Year, t.Month, t.Day, 0, 0, 0).AddDays(1) - DateTime.Now).Ticks * 0.1f;
            //uptime += 10000f;
            string today = string.Empty;
            switch (t.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    today = "일요일";
                    break;
                case DayOfWeek.Monday:
                    today = "월요일";
                    break;
                case DayOfWeek.Tuesday:
                    today = "화요일";
                    break;
                case DayOfWeek.Wednesday:
                    today = "수요일";
                    break;
                case DayOfWeek.Thursday:
                    today = "목요일";
                    break;
                case DayOfWeek.Friday:
                    today = "금요일";
                    break;
                case DayOfWeek.Saturday:
                    today = "토요일";
                    break;

            }
            this.Text = $"{t.Year}년 {t.Month}월 {t.Day}일 " + today;
        }
        base.Update(ms);
    }

    public override void Resize()
    {
        this.Size = (int)(20 * Window.AppropriateSize);
        base.Resize();
    }
}

class TimeText : TextboxForAnimation
{
    public TimeText() : base("cache/font.ttf",46, "시간 검색중") {
        //this.FontColor = new(250, 250, 180);
    }

    float uptime = 0f;

    public override void Update(float ms)
    {
        if (uptime < Framework.RunningTime)
        {
            uptime += 100f;
            var t = DateTime.Now;
            this.Text = $"{t.Hour}:{t.Minute.ToString("00")}:{t.Second.ToString("00")}";
        }
        base.Update(ms);
    }

    public override void Resize()
    {
        this.Size = (int)(46 * Window.AppropriateSize);
        base.Resize();
    }
}

class BatteryStatus : TextboxForAnimation
{
    public BatteryStatus() : base("cache/font.ttf",14, "정보 확인중...") {
        this.OriginY = VerticalPositionType.Bottom;
        this.DrawY = VerticalPositionType.Top;
        this.Y = -6;
    }

    void UpEnd()
    {
        top = false;
        this.MoveAnimationState.CompleteFunction = DownEnd;
        this.Move(null, (int)(-6 * Window.AppropriateSize), MainScene.MonitoringTextPositionChangeTime);
    }

    void DownEnd()
    {
        top = true;
        this.MoveAnimationState.CompleteFunction = UpEnd;
        this.Move(null, (int)(Window.Height * -0.15f), MainScene.MonitoringTextPositionChangeTime);
    }

    public override void Start()
    {
        base.Start();
        DownEnd();
    }

    float Uptime = 0f;

    bool top = true;

    BatteryStatusInfo info;

    public override void Update(float ms)
    {
        if (Uptime < Framework.RunningTime) {
            Uptime += 1500f;
            info = Program.GetBattery();
            switch(info.bs)
            {
                case 2:
                    this.Text = $"전원 연결됨 (배터리 잔량: {info.ecr}%)";
                    break;

                case 3:
                    this.Text = "충전이 완료되었습니다.";
                    break;
                case 4:
                    this.Text = $"배터리가 부족합니다. 전원을 연결하세요. (배터리 잔량: {info.ecr}%, 남은 시간: {info.ecr}분)";
                    break;
                case 5:
                    this.Text = $"배터리가 매우 부족합니다. 전원을 연결하세요. (배터리 잔량: {info.ecr}%, 남은 시간: {info.ecr}분)";
                    break;
                case 9:
                case 8:
                case 7:
                case 6:
                    this.Text = $"충전중입니다. (배터리 잔량 {info.ecr}%)";
                    break;
                default:
                    this.Text = $"배터리 잔량: {info.ecr}% (남은 시간: {info.ecr}분)";
                    break;
            }
        }
        base.Update(ms);
    }

    public override void Resize()
    {
        this.Size = (int)(14 * Window.AppropriateSize);
        if (top) this.MoveAnimationState.ModifyArrivalPoint(null, (int)(Window.Height * -0.15f));
        else this.MoveAnimationState.ModifyArrivalPoint(null, (int)(-6 * Window.AppropriateSize));
        base.Resize();
    }
}

class CPUcounter : TextboxForAnimation
{
    public CPUcounter() : base("cache/font.ttf", 10, "CPU 사용량 확인중...")
    {
        this.OriginX = HorizontalPositionType.Left;
        this.DrawX = HorizontalPositionType.Right;
        this.OriginY = VerticalPositionType.Top;
        this.DrawY = VerticalPositionType.Bottom;
        this.X  = this.Y = 8;
    }

    private PerformanceCounter theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

    public override void Resize()
    {
        this.Size = (int)(10 * Window.AppropriateSize);
        this.Y = (int)(8 * Window.AppropriateSize);
        if (right) this.X = -this.Y;
        else this.X = this.Y;
        base.Resize();
    }

    float uptime = 0f, positionchangetime = MainScene.MonitoringTextPositionChangeTime;
    bool right = false;

    public override void Update(float ms)
    {
        if (uptime < Framework.RunningTime)
        {
            uptime += 1000f;
            this.Text = $"CPU 사용량: {theCPUCounter.NextValue()}%";
            if (positionchangetime < Framework.RunningTime)
            {
                positionchangetime += MainScene.MonitoringTextPositionChangeTime;
                this.OpacityAnimationState.CompleteFunction = () =>
                {
                    if (right = !right)
                    {
                        this.OriginX = HorizontalPositionType.Right;
                        this.DrawX = HorizontalPositionType.Left;
                    }
                    else
                    {
                        this.OriginX = HorizontalPositionType.Left;
                        this.DrawX = HorizontalPositionType.Right;
                    }
                    Resize();
                    this.Opacity(255, 200f);
                    this.OpacityAnimationState.CompleteFunction = () => this.OpacityAnimationState.CompleteFunction = null;
                };
                this.Opacity(0, 200f);
            }
        }
        base.Update(ms);
    }
}

class Ramcounter : TextboxForAnimation
{
    public Ramcounter() : base("cache/font.ttf", 10, "메모리 사용량 확인중...")
    {
        this.OriginX = HorizontalPositionType.Left;
        this.DrawX = HorizontalPositionType.Right;
        this.OriginY = VerticalPositionType.Top;
        this.DrawY = VerticalPositionType.Bottom;
        this.X = 8; this.Y = 18;
    }

    private PerformanceCounter theMemCounter =
   new PerformanceCounter("Memory", "Available MBytes");

    public override void Resize()
    {
        this.Size = (int)(10 * Window.AppropriateSize);
        this.X = (int)(8 * Window.AppropriateSize) * (right ? -1 : 1);
        this.Y = (int)(18 * Window.AppropriateSize);
        base.Resize();
    }

    float uptime, positionchangetime = MainScene.MonitoringTextPositionChangeTime;
    bool right = false;

    public override void Update(float ms)
    {
        if (uptime < Framework.RunningTime)
        {
            uptime += 1000f;

            this.Text = $"여유 메모리: {theMemCounter.NextValue()}MB";

        }
        if (positionchangetime < Framework.RunningTime)
        {
            positionchangetime += MainScene.MonitoringTextPositionChangeTime;
            this.OpacityAnimationState.CompleteFunction = () =>
            {
                if (right = !right)
                {
                    this.OriginX = HorizontalPositionType.Right;
                    this.DrawX = HorizontalPositionType.Left;
                }
                else
                {
                    this.OriginX = HorizontalPositionType.Left;
                    this.DrawX = HorizontalPositionType.Right;
                }
                Resize();
                this.OpacityAnimationState.CompleteFunction = () => this.OpacityAnimationState.CompleteFunction = null;
                this.Opacity(255, 200f);
            };
            this.Opacity(0, 200f);
        }
        base.Update(ms);
    }
}

class BluetoothList : TextboxForAnimation
{
    public BluetoothList() : base("cache/font.ttf", 10, "블루투스 확인중...")
    {
        this.OriginX = HorizontalPositionType.Right;
        this.DrawX = HorizontalPositionType.Left;
        this.OriginY = VerticalPositionType.Bottom;
        this.DrawY = VerticalPositionType.Top;
        this.X = this.Y = -8;
    }

    float Uptime = 0f;
    float positionchangetime = MainScene.MonitoringTextPositionChangeTime;
    bool right = true;

    public override async void Update(float ms)
    {
        if (Uptime < Framework.RunningTime)
        {
            DeviceInformationCollection ConnectedBluetoothDevices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected));
            if (ConnectedBluetoothDevices.Count == 0) this.Text = "(현재 연결된 블루투스 기기 없음)";
            else if (ConnectedBluetoothDevices.Count == 1) this.Text = $"연결된 블루투스 기기: {ConnectedBluetoothDevices[0].Name}";
            else this.Text = $"연결된 블루투스 기기: {ConnectedBluetoothDevices.Count}개 연결됨";
            Uptime += 2000f;

        }
        if (positionchangetime < Framework.RunningTime)
        {
            positionchangetime += MainScene.MonitoringTextPositionChangeTime;
            this.OpacityAnimationState.CompleteFunction = () =>
            {
                if (right = !right)
                {
                    this.OriginX = HorizontalPositionType.Right;
                    this.DrawX = HorizontalPositionType.Left;
                } else
                {
                    this.OriginX = HorizontalPositionType.Left;
                    this.DrawX = HorizontalPositionType.Right;
                }
                Resize();
                this.OpacityAnimationState.CompleteFunction = () => this.OpacityAnimationState.CompleteFunction = null;
                this.Opacity(255, 200f);
            };
            this.Opacity(0, 200f);
        }
        base.Update(ms);    
    }

    public override void Resize()
    {
        this.Size = (int)(10 * Window.AppropriateSize);
        this.Y = (int)(-8 * Window.AppropriateSize);
        if (right) this.X = this.Y;
        else this.X = -this.Y;
        base.Resize();
    }
}

class Clock : Scene
{
    TimeText tt;
    DateText dt;

    public Clock()
    {
        this.AddSprite(tt = new TimeText());
        this.AddSprite(dt = new DateText());
        way = r.Next(1) == 0;
        this.Hide = true;
    }

    Random r = new();

    bool way;
    bool top = true;

    float uptime = 0f;

    int half = (int)(Window.Width * 0.5f);

    public override void Start()
    {
        DownEnd();
        base.Start();
    }

    void UpEnd()
    {
        top = false;
        tt.Move(null, (int)(Window.Height * 0.1f), 3600000f);
        dt.Move(null, (int)(Window.Height * 0.1f) + (int)(40 * Window.AppropriateSize), 3600000f);
        tt.MoveAnimationState.CompleteFunction = DownEnd;
    }

    void DownEnd()
    {
        top = true;
        dt.Move(null, (int)(Window.Height * -0.5f) + (int)(40 * Window.AppropriateSize), 3600000f);
        tt.Move(null, (int)(Window.Height * -0.5f), 3600000f);
        tt.MoveAnimationState.CompleteFunction = UpEnd;
    }

    public override void Resize()
    {
        half = (int)(Window.Width * 0.5f);
        if (top)
        {
            tt.MoveAnimationState.ModifyArrivalPoint(null, (int)(Window.Height * -0.5f));
            dt.MoveAnimationState.ModifyArrivalPoint(null, (int)(Window.Height * -0.5f) + (int)(40 * Window.AppropriateSize));
        } else
        {
            tt.MoveAnimationState.ModifyArrivalPoint(null, (int)(Window.Height * 0.1f));
            dt.MoveAnimationState.ModifyArrivalPoint(null, (int)(Window.Height * 0.1f) + (int)(40 * Window.AppropriateSize));
        }
        base.Resize();
    }

    public override void Update(float ms)
    {
        if (uptime < Framework.RunningTime)
        {
            uptime += 60000f;
            if (way)
            {
                tt.X--; dt.X--;
                if (tt.X < -half) way = false;
            } else
            {
                tt.X++; dt.X++;
                if (tt.X > half) way = true;
            }
        } 
        base.Update(ms);
    }
}

class MainScene : Scene
{
    public const float MonitoringTextPositionChangeTime = 600000f;

    public MainScene()
    {
        this.Hide = true;
        this.AddSprite(new BatteryStatus());
        this.AddSprite(new BluetoothList());
        this.AddSprite(new CPUcounter());
        this.AddSprite(new Ramcounter());
    }

    public override void Start()
    {
        base.Start();
        //Jyunrcaea! Framework 0.5.x 에 구현할 기능
        SDL2.SDL.SDL_ShowCursor(0);
    }

    public override void WindowQuit()
    {
        base.WindowQuit();
        Framework.Stop();
    }

    public static bool wf = false;

    public override void KeyDown(Keycode e)
    {
        base.KeyDown(e);
        if (e == Keycode.F11)
        {
            wf = true;
            Window.Fullscreen = !Window.Fullscreen;
        }
        else if (e == Keycode.ESCAPE)
            Framework.Stop();
        else if (e == Keycode.F3)
            Framework.ObjectDrawDebuging = !Framework.ObjectDrawDebuging;
    }
}

class WindowState : Scene
{
    StateText t;
    StateBackground b;

    public WindowState()
    {
        this.AddSprite(b = new());
        this.AddSprite(t = new());

        t.Opacity(0); b.Opacity(0);
    }

    public override void Start()
    {
        base.Start();
        t.Resize();
        b.Resize();
    }

    public override void Resized()
    {
        t.Y = b.Y = (int)(Window.Height * 0.2f);
        t.Size = (int)(24 * Window.AppropriateSize);
        b.Width = (int)(400 * Window.AppropriateSize);
        b.Height = t.Size * 2;
        if (MainScene.wf)
        {
            MainScene.wf = false;
            Show($"창 모드 변경됨: {(Window.Fullscreen ? "전체화면" : "창화면")} ({Window.Width} × {Window.Height})");
        }
        else Show($"창 크기 조절됨: {Window.Width} × {Window.Height}");
    }

    public void Show(string text)
    {
        t.Text = text;
        t.Opacity(200, 200f);
        b.Opacity(200, 200f);
    }
}

class StateText : TextboxForAnimation
{
    public StateText() : base("cache\\font.ttf", 0)
    {
        this.FontColor = new(0, 0, 0);
        this.OpacityAnimationState.CompleteFunction = () =>
        {
            this.Opacity(0, 200f, 500f);
        };
    }

}

class StateBackground : RectangleForAnimation
{
    public StateBackground()
    {
        this.OpacityAnimationState.CompleteFunction = () =>
        {
            this.Opacity(0, 200f, 500f);
        };
    }
}

class curt : Scene
{
    backgroundcolor c;

    public curt()
    {
        this.AddSprite(c = new(new(0, 0, 0)));
    }

    public override void Start()
    {
        base.Start();
        Program.cc.Hide = Program.ms.Hide = false;
    }

    public void Finish()
    {
        c.OpacityAnimationState.CompleteFunction = () =>
        {
            Display.RemoveScene(this);
        };
        c.Opacity(0, 200f);
    }
}

class Welcome : Scene
{
    backgroundcolor c;
    welcometext t;

    public Welcome()
    {
        this.AddSprite(c = new backgroundcolor(new()));
        this.AddSprite(t = new welcometext());
    }

    public void Finish()
    {
        t.OpacityAnimationState.CompleteFunction = () =>
        {
            Display.RemoveScene(this);
            Program.ct.Finish();
        };
        c.Opacity(0, 200f);
        t.Opacity(0, 200f);
    }
}

class backgroundcolor : RectangleForAnimation
{
    public backgroundcolor(Color color) : base(Window.Width,Window.Height)
    {
        this.Color = color;
    }

    public override void Start()
    {
        base.Start();
    }

    public override void Resize()
    {
        Width = Window.Width;
        Height = Window.Height;
        base.Resize();
    }
}

class welcometext : SpriteForAnimation
{
    public welcometext() : base(new TextureFromText("cache\\font.ttf", 48, "Always On Display", new(0,0,0))) {

    }

    public override void Start()
    {
        base.Start();
    }

    public override void Resize()
    {
        this.Size = Window.AppropriateSize;
        base.Resize();
    }
}
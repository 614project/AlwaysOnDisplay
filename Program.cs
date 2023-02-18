using JyunrcaeaFramework;
using System.Management;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using System.Diagnostics;
using AlwaysOnDisplay;
using Windows.UI.Notifications.Management;
using Windows.UI.Notifications;
using Windows.Foundation.Metadata;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

class Program
{
    public static Clock cc = null!;
    public static MainScene ms = null!;
    public static Welcome wc = null!;
    public static ColorFill cf = null!;
    
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
        if (!File.Exists("cache\\icon.png"))
        {
            AlwaysOnDisplay.Properties.Resources.Jyunrcaea_FrameworkIcon.Save("cache\\icon.png");
        }
        if (!File.Exists("cache\\color.png"))
        {
            AlwaysOnDisplay.Properties.Resources.colorer.Save("cache\\color.png");
        }
        Framework.MultiCoreProcess = true;
        Framework.BackgroundColor = new(0, 0, 0);
        Framework.Init("Always On Display", 960, 614, null, null, new(true,false,false,true));
        Window.Opacity = 0f;
        Framework.Function = new CustomFF();
        Display.AddScene(wc = new Welcome());
        Task.Run(() =>
        {
            Window.Icon("cache\\icon.png");
            Display.AddScene(ms = new MainScene());
            Display.AddScene(cc = new Clock());
            Display.AddScene(cf = new());
            Display.AddScene(new WindowStatus());
            Framework.Function.Resize();
            Display.RemoveScene(wc);
            ms.Hide = cc.Hide = cf.Hide = false;
        });
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

class CustomFF : FrameworkFunction
{
    public override void Start()
    {
        base.Start();
        Task.Run(() =>
        {
            Window.Show = true;
            Mouse.HideCursor = true;
            Window.Raise();
            while (Window.Opacity < 1f)
            {
                Window.Opacity += 0.01f;
                Thread.Sleep(10);
            }
        });
    }



    public override void WindowQuit()
    {
        base.WindowQuit();
        Framework.Stop();
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

    BatteryStatusInfo info = null!;

    public override async void Update(float ms)
    {
        if (Uptime < Framework.RunningTime) await Task.Run(() =>
        {
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
                    this.Text = $"배터리가 부족합니다. 전원을 연결하세요. (배터리 잔량: {info.ecr}%, 남은 시간: {info.ert}분)";
                    break;
                case 5:
                    this.Text = $"배터리가 매우 부족합니다. 전원을 연결하세요. (배터리 잔량: {info.ecr}%, 남은 시간: {info.ert}분)";
                    break;
                case 9:
                case 8:
                case 7:
                case 6:
                    this.Text = $"충전중입니다. (배터리 잔량 {info.ecr}%)";
                    break;
                default:
                    this.Text = $"배터리 잔량: {info.ecr}% (남은 시간: {info.ert}분)";
                    break;
            }
        });
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
            uptime += 500f;
            
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
    public BluetoothList() : base("cache/font.ttf", 12, "블루투스 확인중...")
    {
        this.OriginX = HorizontalPositionType.Right;
        this.DrawX = HorizontalPositionType.Left;
        this.OriginY = VerticalPositionType.Bottom;
        this.DrawY = VerticalPositionType.Top;
        this.X = this.Y = -8;
        this.Blended = true;
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
        this.Size = (int)(12 * Window.AppropriateSize);
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
        dt.Move(null, (int)(Window.Height * -0.25f) + (int)(40 * Window.AppropriateSize), 3600000f);
        tt.Move(null, (int)(Window.Height * -0.25f), 3600000f);
        tt.MoveAnimationState.CompleteFunction = UpEnd;
    }

    public override void Resize()
    {
        half = (int)(Window.Width * 0.5f);
        if (top)
        {
            tt.MoveAnimationState.ModifyArrivalPoint(null, (int)(Window.Height * -0.25f));
            dt.MoveAnimationState.ModifyArrivalPoint(null, (int)(Window.Height * -0.25f) + (int)(40 * Window.AppropriateSize));
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
        this.AddSprite(new NoticeText());
        //this.AddSprite(new PlayingMusic());
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

class WindowStatus : Scene
{
    StateText t;
    StateBackground b;

    public WindowStatus()
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
        Show("'F11' 키를 눌러 전체화면으로 전환할수 있습니다.");
    }

    public override void Resized()
    {
        t.Y = b.Y = (int)(Window.Height * 0.2f);
        t.Size = (int)(18 * Window.AppropriateSize);
        b.Width = (int)(300 * Window.AppropriateSize);
        b.Height = (int)(t.Size * 1.8f);
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
        t.Hide = b.Hide = false;
        t.Opacity(180, 200f);
        b.Opacity(180, 200f);
    }
}

class StateText : TextboxForAnimation
{
    public StateText() : base("cache\\font.ttf", 0)
    {
        this.FontColor = new(0, 0, 0);
        this.OpacityAnimationState.CompleteFunction = () =>
        {
            if (this.OpacityAnimationState.TargetOpacity == 0) this.Hide = true;
            else this.Opacity(0, 400f, 500f);
        };
    }

}

[Obsolete("재생중인지 확인하는 객체인데... 어딘가 이상함")]
class PlayingMusic : TextboxForAnimation
{
    MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
    MMDevice device;

    AudioSessionControl session = null!;

    public PlayingMusic() : base("cache\\font.ttf",0)
    {
        device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        for(int i =0; i < device.AudioSessionManager.Sessions.Count; i++)
        {
            if (device.AudioSessionManager.Sessions[i].IsSystemSoundsSession == false && device.AudioSessionManager.Sessions[i].State == AudioSessionState.AudioSessionStateActive)
            {
                session = device.AudioSessionManager.Sessions[i]; break;
            }
        }
    }

    float uptime = 0f;

    public override void Resize()
    {
        this.Size = (int)(16 * Window.AppropriateSize);
        this.Y = (int)(Window.Height * 0.3f);
        base.Resize();
    }

    public override void Update(float ms)
    {
        if (uptime < Framework.RunningTime)
        {
            uptime += 2000f;
            if (session != null)
            {
                string sessionName = session.DisplayName;
                string processName = Process.GetProcessById((int)session.GetProcessID).ProcessName;
                //var state = Process.GetProcessById((int)session.GetProcessID).;

                // Print the information to the console
                this.Text = "현재 재생중: " + sessionName + " (프로그램 '" + processName + "' 에서)";
            }
            else
            {
                this.Text = "재생중인 노래가 없습니다.";
            }
        }
        base.Update(ms);
    }
}

class StateBackground : RectangleForAnimation
{
    public StateBackground()
    {
        this.OpacityAnimationState.CompleteFunction = () =>
        {
            if (this.OpacityAnimationState.TargetOpacity == 0) this.Hide = true;
            else this.Opacity(0, 400f, 500f);
        };
    }
}

class Welcome : Scene
{
    public Welcome()
    {
        this.AddSprite(new backgroundcolor());
        this.AddSprite(new welcometext());
        this.AddSprite(new Madeby());
        this.AddSprite(new JF());
    }

}

class backgroundcolor : Rectangle
{
    public backgroundcolor() : base(Window.Width,Window.Height)
    {

    }

    public override void Resize()
    {
        Width = Window.Width;
        Height = Window.Height;
        base.Resize();
    }
}

class welcometext : Sprite
{
    public welcometext() : base(new TextureFromText("cache\\font.ttf", 50, "Always On Display", new(0,0,0))) {
        //this.Size = 0.5f;
    }

    public override void Resize()
    {
        this.Size = Window.AppropriateSize;
        base.Resize();
    }
}

class JF : Sprite
{
    public JF() : base(new TextureFromStringForXPM(JFImage.result))
    {
        this.OriginY = VerticalPositionType.Bottom;
        this.DrawY = VerticalPositionType.Top;
        this.Y = -8;
        this.Size = 0.5f;
    }

    public override void Resize()
    {
        this.Size = 0.5f * Window.AppropriateSize;
        base.Resize();
    }
}

class Madeby : Sprite
{
    public Madeby() : base(new TextureFromText("cache\\font.ttf",18,"Made by 614project",new(0,0,0))) {
        this.OriginX = HorizontalPositionType.Right;
        this.DrawX = HorizontalPositionType.Left;
        this.OriginY = VerticalPositionType.Bottom;
        this.DrawY = VerticalPositionType.Top;
        this.X =  this.Y = -8;
    }

    public override void Resize()
    {
        this.Size = Window.AppropriateSize;
        base.Resize();
    }
}

class NoticeText : TextboxForAnimation, KeyDownEventInterface
{
    public NoticeText() : base("cache\\font.ttf",16,"활성화 된 알림은 'Backspace' 키로 삭제하실수 있습니다.")
    {
        this.Opacity(0);
        this.OpacityAnimationState.CompleteFunction = () =>
        {
            if (this.OpacityAnimationState.TargetOpacity == 0)
            {
                this.Hide = true;
                removed = false;
                this.needsetopacity = true;
            } else
            {
                this.needsetopacity = false;
            }
        };
        this.MoveAnimationState.CompleteFunction = () =>
        {
            Move((int)(Window.Width * ((right = !right) ? 0.2f : -0.2f)), null, MainScene.MonitoringTextPositionChangeTime * 2);
        };
    }

    bool right = new Random().Next(1) == 0;

    UserNotificationListener listener = UserNotificationListener.Current;
    UserNotificationListenerAccessStatus accessStatus;

    IReadOnlyList<UserNotification> notifs = null!;

    public override async void Start()
    {
        base.Start();
        if (!ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener")) ((Scene)this.InheritedObject!).RemoveSprite(this);
        accessStatus = await listener.RequestAccessAsync();
        if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
        {
            ((Scene)this.InheritedObject!).RemoveSprite(this);
        }
        Move((int)(Window.Width * (right ? 0.2f : -0.2f)), null, MainScene.MonitoringTextPositionChangeTime);
        Update(0);
    }

    public override void Resize()
    {
        this.Size = (int)(16 * Window.AppropriateSize);
        this.Y = (int)(Window.Height * 0.25f);
        this.MoveAnimationState.ModifyArrivalPoint((int)(Window.Width * (right ? 0.2f : -0.2f)), null);
        base.Resize();
    }

    float uptime = 0f;

    int i,j;

    string info = string.Empty;

    bool removed = false,needsetopacity = true;

    public void KeyDown(Keycode key)
    {
        if (notifs.Count == 0 || key != Keycode.BACKSPACE) return;
        removed = true;
        this.Text = "(알림을 삭제했습니다.)";
        for (j = 0; j < notifs.Count; j++)
        {
            listener.RemoveNotification(notifs[j].Id);
        }
        this.Opacity(0, 300f, 1000f);
    }

    public override async void Update(float ms)
    {
        if (uptime < Framework.RunningTime)
        {
            uptime += 2000f;
            if (removed) return;
            notifs = await listener.GetNotificationsAsync(NotificationKinds.Toast);
            if (notifs.Count == 0)
            {
                this.Opacity(0, 300f);
            }
            else
            {
                this.Hide = false;
                if (notifs.Count < 3)
                {
                    info = "알림: ";
                    for (i = 0; i < notifs.Count; i++)
                    {
                        info += notifs[i].AppInfo.DisplayInfo.DisplayName;
                        if (i < notifs.Count - 1)
                        {
                            info += ", ";
                        }
                    }
                    this.Text = info;
                }
                else
                {
                    this.Text = $"{notifs.Count}개의 알림이 왔습니다.";
                }
                if (needsetopacity)
                {
                    this.Opacity(255, 300f);
                }
            }
        }
        base.Update(ms);
    }
}

class ColorFill : Canvas
{
    TextureFromFile t;
    RectSize s = new();

    bool opup = false;

    public ColorFill()
    {
        AddUsingTexture(t = new("cache\\color.png"));
        t.Opacity = 80;
        this.Hide = true;
    }

    public override void Resize()
    {
        s.Width = Window.Width;
        s.Height = Window.Height;
        base.Resize();
    }

    public override void Render()
    {
        Renderer.BlendMode(Renderer.BlendType.Mul);
        Renderer.Texture(t, s);
    }

    float uptime = 0f;

    public override void Update(float millisecond)
    {
        if (uptime > Framework.RunningTime) return;
        uptime += 100f;
        if (opup)
        {
            this.t.Opacity++;
            if (this.t.Opacity >= 100) { opup = false; uptime += 800f; }
        } else
        {
            this.t.Opacity--;
            if (this.t.Opacity <= 0) { opup = true; uptime += 500f; }
        }
    }
}
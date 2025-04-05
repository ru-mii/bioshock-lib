using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

public class Main
{
    Process Instance = null;
    ulong ModBase = 0x10900000;
    public ulong Multiplier1 = 0;
    public ulong Multiplier2 = 0;
    bool StopThread = false;

    int GameStage = 0;
    ulong SpeedLastTime = 0;
    ulong SpeedStartTime = 0;
    public ulong SpeedTime = 0;

    public bool Ready = false;

    public bool LiveSplitRunning = false;
    Stopwatch Watcher = Stopwatch.StartNew();

    public Main()
    {
        Tools.OutputDebug("bioshock-lib loaded");
        Tools.SaveRandomID(Guid.NewGuid().ToString());

        Thread abc1 = new Thread(T_CheckRepeat);
        abc1.IsBackground = true;
        abc1.Start();

        Thread abc2 = new Thread(T_Main);
        abc2.IsBackground = true;
        abc2.Start();
    }

    public void SetProcess(Process process)
    {
        Instance = process;
    }

    public void ResetValues()
    {
        GameStage = 0;
        SpeedLastTime = 0;
        SpeedStartTime = 0;
        SpeedTime = 0;
    }

    private void T_CheckRepeat()
    {
        while (true)
        {
            try
            {
                string prev = Tools.GetRandomID();
                string curr = Tools.RandomID;

                if (curr != prev)
                {
                    StopThread = true;
                    Watcher.Stop();
                    return;
                }

                Thread.Sleep(1000);
            }
            catch { }
        }
    }

    private void T_Main()
    {
        while(true)
        {
            if (StopThread) return;

            try
            {
                if (Instance != null && !Instance.HasExited && LiveSplitRunning && Ready)
                {
                    float[] position = new float[0];
                    if (GameStage != -1) position = GetPosition();

                    if (GameStage == 0)
                    {
                        if (position.Length > 0 && position[2] < -300)
                        {
                            Multiplier1 = 10;
                            SetSpeed(Multiplier1);
                            SpeedLastTime = SpeedTime;
                            SpeedStartTime = (ulong)Watcher.ElapsedMilliseconds;
                            GameStage = 1;
                        }
                    }

                    if (GameStage == 1)
                    {
                        if (position.Length > 0)
                        {
                            ulong singularity = (ulong)Watcher.ElapsedMilliseconds - SpeedStartTime;
                            SpeedTime = (singularity * Multiplier1) - singularity + SpeedLastTime;
                        }

                        if (position.Length > 0 && position[0] > 75000)
                        {
                            SpeedTime -= 60;
                            SetSpeed(1);
                            GameStage = 2;
                        }
                    }
                    else if (GameStage == 2)
                    {
                        if (GetLoading() != 0)
                        {
                            GameStage = 3;
                        }
                    }

                    if (GameStage == 3)
                    {
                        if (GetLoading() == 0)
                        {
                            GameStage = 4;
                        }
                    }

                    if (GameStage == 4)
                    {
                        if (GetLoading() == 0 && GetHUD() == 2 && position.Length > 0 && position[2] > -1215)
                        {
                            Multiplier2 = 10;
                            SetSpeed(Multiplier2);
                            SpeedLastTime = SpeedTime;
                            SpeedStartTime = (ulong)Watcher.ElapsedMilliseconds;
                            GameStage = 5;
                        }
                    }

                    if (GameStage == 5)
                    {
                        ulong singularity = (ulong)Watcher.ElapsedMilliseconds - SpeedStartTime;
                        SpeedTime = (singularity * Multiplier2) - singularity + SpeedLastTime;

                        if (GetHUD() == 0)
                        {
                            SetSpeed(1);
                            GameStage = -1;

                            double finalEdited = ((double)SpeedTime - SpeedLastTime) - 59340;
                            if (finalEdited < 50000 && finalEdited > 0) SpeedTime -= (ulong)finalEdited;
                        }
                    }
                }
                else
                {
                    ResetValues();
                    Ready = false;
                    for (int i = 0; i < 10; i++) Thread.Sleep(100);
                }
                Thread.Sleep(1);
            }
            catch { Thread.Sleep(100); }
        }
    }

    private int GetHUD()
    {
        try
        {
            ulong address = ModBase;

            if (address == 0) return 0;
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0x9C4970);
            if (address == 0) return 0;
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0x38);
            if (address == 0) return 0;
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0xEC);
            if (address == 0) return 0;

            int toRet = Tools.ReadMemory<int>(Instance, address + 0xAA4);
            return toRet;
        }
        catch { }
        return 0;
    }

    private float[] GetPosition()
    {
        try
        {
            ulong address = ModBase;

            if (address == 0) return new float[0];
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0x9C4970);
            if (address == 0) return new float[0];
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0x38);
            if (address == 0) return new float[0];
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0x8C);
            if (address == 0) return new float[0];

            byte[] bytes = Tools.ReadMemoryBytes(Instance, address + 0x1C0, 12);
            return new float[]
            {
                BitConverter.ToSingle(bytes, 0),
                BitConverter.ToSingle(bytes, 4),
                BitConverter.ToSingle(bytes, 8),
            };
        }
        catch { }
        return new float[0];
    }

    private float GetSpeed()
    {
        try
        {
            ulong address = ModBase;

            if (address == 0) return 0;
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0x8D1988);
            if (address == 0) return 0;
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0x1C);
            if (address == 0) return 0;
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0xE0);
            if (address == 0) return 0;

            float toRet = (ulong)Tools.ReadMemory<float>(Instance, address + 0x5BC);
            return toRet;
        }
        catch { }
        return 0;
    }

    private void SetSpeed(float speed)
    {
        try
        {
            ulong address = ModBase;

            if (address == 0) return;
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0x8D1988);
            if (address == 0) return;
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0x1C);
            if (address == 0) return;
            address = (ulong)Tools.ReadMemory<int>(Instance, address + 0xE0);
            if (address == 0) return;

            Tools.WriteMemoryBytes(Instance, address + 0x5BC, BitConverter.GetBytes(speed));
        }
        catch { }
    }

    private int GetLoading()
    {
        try
        {
            ulong address = ModBase;
            return Tools.ReadMemory<int>(Instance, address + 0x8D666C);
        }
        catch { }
        return 0;
    }
}
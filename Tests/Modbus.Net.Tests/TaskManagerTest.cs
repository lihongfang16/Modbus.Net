﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Modbus.Net.Modbus;

namespace Modbus.Net.Tests
{
    [TestClass]
    public class TaskManagerTest
    {
        private TaskManager _taskManager;

        private Dictionary<string, double> _valueDic = new Dictionary<string, double>();

        private Timer _timer;

        [TestInitialize]
        public void TaskManagerInit()
        {
            _taskManager = new TaskManager(20, true);

            var addresses = new List<AddressUnit>
            {
                new AddressUnit
                {
                    Id = "0",
                    Area = "4X",
                    Address = 2,
                    SubAddress = 0,
                    CommunicationTag = "A1",
                    DataType = typeof(ushort)
                },
                new AddressUnit
                {
                    Id = "1",
                    Area = "4X",
                    Address = 3,
                    SubAddress = 0,
                    CommunicationTag = "A2",
                    DataType = typeof(ushort)
                },
                new AddressUnit
                {
                    Id = "2",
                    Area = "4X",
                    Address = 4,
                    SubAddress = 0,
                    CommunicationTag = "A3",
                    DataType = typeof(ushort)
                },
                new AddressUnit
                {
                    Id = "3",
                    Area = "4X",
                    Address = 5,
                    SubAddress = 0,
                    CommunicationTag = "A4",
                    DataType = typeof(ushort)
                },
                new AddressUnit
                {
                    Id = "4",
                    Area = "4X",
                    Address = 6,
                    SubAddress = 0,
                    CommunicationTag = "A5",
                    DataType = typeof(uint)
                },
                new AddressUnit
                {
                    Id = "5",
                    Area = "4X",
                    Address = 8,
                    SubAddress = 0,
                    CommunicationTag = "A6",
                    DataType = typeof(uint)
                }
            };

            BaseMachine machine = new ModbusMachine(ModbusType.Tcp, "192.168.3.10", addresses, true, 2, 0);

            _taskManager.AddMachine(machine);

            var r = new Random();

            _timer = new Timer(state =>
            {
                lock (_valueDic)
                {
                    _valueDic = new Dictionary<string, double>
                    {
                        {
                            "A1", r.Next(0, UInt16.MaxValue)
                        },
                        {
                            "A2", r.Next(0, UInt16.MaxValue)
                        },
                        {
                            "A3", r.Next(0, UInt16.MaxValue)
                        },
                        {
                            "A4", r.Next(0, UInt16.MaxValue)
                        },
                        {
                            "A5", r.Next()
                        },
                        {
                            "A6", r.Next()
                        }
                    };
                }
            }, null, 0, 1000);

            _taskManager.InvokeTimerAll(new TaskItemSetData(_valueDic, MachineSetDataType.CommunicationTag)
            {
                TimerTime = 2000,
                TimeoutTime = 60000,
                TimerDisconnectedTime = 10000
            });
        }

        [TestMethod]
        public void TaskManagerValueReadWriteTest()
        {
            var dicans = new Dictionary<string, double?>();
            _taskManager.InvokeTimerAll(new TaskItemGetData(
                def =>
                {
                    dicans = def.ReturnValues.ToDictionary(p => p.Key, p => p.Value.PlcValue);
                }, MachineGetDataType.CommunicationTag, 2000, 10000, 60000));

            var i = 5;
            while (i > 0)
            {
                Thread.Sleep(10000);
                lock (dicans)
                {
                    lock (_valueDic)
                    {
                        Assert.AreEqual(dicans["A1"], _valueDic["A1"]);
                        Assert.AreEqual(dicans["A2"], _valueDic["A2"]);
                        Assert.AreEqual(dicans["A3"], _valueDic["A3"]);
                        Assert.AreEqual(dicans["A4"], _valueDic["A4"]);
                        Assert.AreEqual(dicans["A5"], _valueDic["A5"]);
                        Assert.AreEqual(dicans["A6"], _valueDic["A6"]);
                    }
                }
                i--;               
            }
        }

        [TestCleanup]
        public void TaskManagerFinilize()
        {
            _taskManager.StopTimerAll();
            _timer.Dispose();
        }
    }
}

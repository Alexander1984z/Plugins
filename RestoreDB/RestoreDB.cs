﻿using System;
using NEL.Simple.SDK.Helper;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using NEL.Simple.SDK;
using Neo.IO.Data.LevelDB;
using Neo.IO.Caching;

namespace Neo.Plugins
{
    public class RestoreDB : Plugin,IRestorePlugin
    {
        public override void Configure()
        {
            Settings.Load(GetNELConfiguration());
        }

        public void Restore()
        {


            using (FileStream fs = new FileStream("release.zip", FileMode.Open, FileAccess.Read, FileShare.Read))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    using (Stream zs = entry.Open())
                    {
                        Console.WriteLine("正在载入"+ entry.Name);
                        BinaryReader b = new BinaryReader(zs);
                        {
                            //查看当前store的高度
                            var curHeight = Store.GetSnapshot().Height;

                            //开始高度
                            var startHeight = b.ReadUInt32();
                            //结束高度
                            var endHeight = b.ReadUInt32();

                            if (curHeight >= endHeight && curHeight != uint.MaxValue)
                            {
                                b.Dispose();
                                return;
                            }

                            if (curHeight < startHeight)
                            {
                                b.Dispose();
                                throw new Exception("missing data");
                            }

                            List<LevelDbOperation> list = new List<LevelDbOperation>();
                            while (true)
                            {
                                //获取到的数据的高度
                                var height = b.ReadUInt32();
                                var count = b.ReadUInt32();
                                for (var i = 0; i < count; i++)
                                {
                                    var lo = LevelDbOperation.Deserialize(ref b);
                                    //执行操作
                                    list.Add(lo);
                                }
                                if (height > curHeight || curHeight == UInt32.MaxValue)
                                {
                                    ExecuteOperation(list);
                                }
                                list.Clear();
                                if (endHeight == height)
                                    break;
                            }

                        }
                        b.Dispose();
                        Console.WriteLine("结束");
                    }
                }
            }
        }

        public void ExecuteOperation(List<LevelDbOperation> list)
        {
            var levelDB = (Store as Neo.Persistence.LevelDB.LevelDBStore).db;

            WriteBatch batch = new WriteBatch();

            foreach (var l in list)
            {
                if (l.state == (byte)TrackState.Added)
                {
                    batch.Put(SliceBuilder.Begin(l.tableid).Add(l.key??new byte[0]), l?.value);
                }
                else if (l.state == (byte)TrackState.Deleted)
                {
                    batch.Delete(SliceBuilder.Begin(l.tableid).Add(l.key ?? new byte[0]));
                }
                else if (l.state == (byte)TrackState.Changed)
                {
                    batch.Put(SliceBuilder.Begin(l.tableid).Add(l.key ?? new byte[0]), l.value);
                }
            }
            levelDB.Write(WriteOptions.Default, batch);
        }
    }
}

﻿using Microsoft.Extensions.Configuration;
using NEL.Simple.SDK.Helper;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Neo.Plugins
{
    internal class Settings
    {
        public string Conn { get; }
        public string DataBase { get; }
        public string Coll_Task { get; }
        public string Coll_Application { get; }
        public string Coll_Block { get; }
        public string Coll_DumpInfo { get; }
        public string Coll_SystemCounter { get; }
        public string[] MongoDbIndex { get; }

        public static Settings Default
        {
            get;
            private set; }

        private Settings(IConfigurationSection section)
        {
            this.Conn = section.GetSection("Conn").Value;
            this.DataBase = section.GetSection("DataBase").Value;
            this.Coll_Task = section.GetSection("Coll_Task").Value;
            this.Coll_Application = section.GetSection("Coll_Application").Value;
            this.Coll_Block = section.GetSection("Coll_Block").Value;
            this.Coll_DumpInfo = section.GetSection("Coll_DumpInfo").Value;
            this.Coll_SystemCounter = section.GetSection("Coll_SystemCounter").Value;
            this.MongoDbIndex = section.GetSection("MongoDbIndexs").GetChildren().Select(p => p.Value).ToArray();
            if (!string.IsNullOrEmpty(this.Conn) && !string.IsNullOrEmpty(this.Conn) && !string.IsNullOrEmpty(this.Conn))
            {
                //创建索引
                for (var i = 0; i < this.MongoDbIndex.Length; i++)
                {
                    SetMongoDbIndex(this.MongoDbIndex[i]);
                }
            }
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }

        public void SetMongoDbIndex(string mongoDbIndex)
        {
            JObject joIndex = JObject.Parse(mongoDbIndex);
            string collName = (string)joIndex["collName"];
            JArray indexs = (JArray)joIndex["indexs"];
            for (var i = 0; i < indexs.Count; i++)
            {
                string indexName = (string)indexs[i]["indexName"];
                string indexDefinition = indexs[i]["indexDefinition"].ToString();
                bool isUnique = false;
                if (indexs[i]["isUnique"] != null)
                    isUnique = (bool)indexs[i]["isUnique"];
                MongoDBHelper.CreateIndex(this.Conn, this.DataBase, collName, indexDefinition, indexName, isUnique);
            }
        }
    }
}
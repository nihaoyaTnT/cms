﻿using System;
using System.Web;
using System.Xml;
using BaiRong.Core.Data;
using BaiRong.Core.Data.Helper;
using BaiRong.Core.Model.Enumerations;
using SiteServer.Plugin;

namespace BaiRong.Core
{
    public class WebConfigUtils
    {
        private const string NameIsProtectData = "IsProtectData";
        private const string NameDatabaseType = "DatabaseType";
        private const string NameConnectionString = "ConnectionString";

        public static string PhysicalApplicationPath { get; }
        public static string ApplicationPath { get; }
        public static EDatabaseType DatabaseType { get; private set; }
        public static string ConnectionString { get; private set; }

        private static IDbHelper _helper;
        public static IDbHelper Helper
        {
            get
            {
                if (_helper != null) return _helper;

                if (DatabaseType == EDatabaseType.MySql)
                {
                    _helper = new Data.MySql();
                }
                else
                {
                    _helper = new SqlServer();
                }
                return _helper;
            }
        }

        static WebConfigUtils()
        {
            string physicalApplicationPath;
            var applicationPath = string.Empty;
            if (HttpContext.Current != null)
            {
                physicalApplicationPath = HttpContext.Current.Request.PhysicalApplicationPath;
                applicationPath = HttpContext.Current.Request.ApplicationPath;
            }
            else
            {
                physicalApplicationPath = Environment.CurrentDirectory;
            }

            if (string.IsNullOrEmpty(applicationPath))
            {
                applicationPath = "/";
            }

            PhysicalApplicationPath = physicalApplicationPath;
            ApplicationPath = applicationPath;

            var isProtectData = false;
            var databaseType = string.Empty;
            var connectionString = string.Empty;
            try
            {
                var doc = new XmlDocument();

                var configFile = PathUtils.Combine(PhysicalApplicationPath, "web.config");

                doc.Load(configFile);

                var appSettings = doc.SelectSingleNode("configuration/appSettings");
                if (appSettings != null)
                {
                    foreach (XmlNode setting in appSettings)
                    {
                        if (setting.Name == "add")
                        {
                            var attrKey = setting.Attributes?["key"];
                            if (attrKey != null)
                            {
                                if (StringUtils.EqualsIgnoreCase(attrKey.Value, NameIsProtectData))
                                {
                                    var attrValue = setting.Attributes["value"];
                                    if (attrValue != null)
                                    {
                                        isProtectData = TranslateUtils.ToBool(attrValue.Value);
                                    }
                                }
                                else if (StringUtils.EqualsIgnoreCase(attrKey.Value, NameDatabaseType))
                                {
                                    var attrValue = setting.Attributes["value"];
                                    if (attrValue != null)
                                    {
                                        databaseType = attrValue.Value;
                                    }
                                }
                                else if (StringUtils.EqualsIgnoreCase(attrKey.Value, NameConnectionString))
                                {
                                    var attrValue = setting.Attributes["value"];
                                    if (attrValue != null)
                                    {
                                        connectionString = attrValue.Value;
                                    }
                                }
                            }
                        }
                    }

                    if (isProtectData)
                    {
                        databaseType = TranslateUtils.DecryptStringBySecretKey(databaseType);
                        connectionString = TranslateUtils.DecryptStringBySecretKey(connectionString);
                    }
                }
            }
            catch
            {
                // ignored
            }

            DatabaseType = EDatabaseTypeUtils.GetEnumType(databaseType);
            ConnectionString = connectionString;
        }

        public static void UpdateWebConfig(bool isProtectData, EDatabaseType databaseType, string connectionString)
        {
            var configFilePath = PathUtils.MapPath("~/web.config");

            var doc = new XmlDocument();
            doc.Load(configFilePath);
            var dirty = false;
            var appSettings = doc.SelectSingleNode("configuration/appSettings");
            if (appSettings != null)
            {
                foreach (XmlNode setting in appSettings)
                {
                    if (setting.Name == "add")
                    {
                        var attrKey = setting.Attributes?["key"];
                        if (attrKey != null)
                        {
                            if (StringUtils.EqualsIgnoreCase(attrKey.Value, NameIsProtectData))
                            {
                                var attrValue = setting.Attributes["value"];
                                if (attrValue != null)
                                {
                                    attrValue.Value = isProtectData.ToString();
                                    dirty = true;
                                }
                            }
                            else if (StringUtils.EqualsIgnoreCase(attrKey.Value, NameDatabaseType))
                            {
                                var attrValue = setting.Attributes["value"];
                                if (attrValue != null)
                                {
                                    attrValue.Value = EDatabaseTypeUtils.GetValue(databaseType);
                                    if (isProtectData)
                                    {
                                        attrValue.Value = TranslateUtils.EncryptStringBySecretKey(attrValue.Value);
                                    }
                                    dirty = true;
                                }
                            }
                            else if (StringUtils.EqualsIgnoreCase(attrKey.Value, NameConnectionString))
                            {
                                var attrValue = setting.Attributes["value"];
                                if (attrValue != null)
                                {
                                    attrValue.Value = connectionString;
                                    if (isProtectData)
                                    {
                                        attrValue.Value = TranslateUtils.EncryptStringBySecretKey(attrValue.Value);
                                    }
                                    dirty = true;
                                }
                            }
                        }
                    }
                }
            }

            if (dirty)
            {
                var writer = new XmlTextWriter(configFilePath, System.Text.Encoding.UTF8)
                {
                    Formatting = Formatting.Indented
                };
                doc.Save(writer);
                writer.Flush();
                writer.Close();
            }

            DatabaseType = databaseType;
            ConnectionString = connectionString;
            _helper = null;
        }

        public static string GetConnectionStringByName(string name)
        {
            var connectionString = string.Empty;
            try
            {
                var doc = new XmlDocument();

                var configFile = PathUtils.Combine(PhysicalApplicationPath, "web.config");

                doc.Load(configFile);

                var appSettings = doc.SelectSingleNode("configuration/appSettings");
                if (appSettings != null)
                {
                    foreach (XmlNode setting in appSettings)
                    {
                        if (setting.Name != "add") continue;

                        var attrKey = setting.Attributes?["key"];
                        if (attrKey == null) continue;

                        if (!StringUtils.EqualsIgnoreCase(attrKey.Value, name)) continue;

                        var attrValue = setting.Attributes["value"];
                        if (attrValue != null)
                        {
                            connectionString = attrValue.Value;
                        }
                        break;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return connectionString;
        }
    }
}
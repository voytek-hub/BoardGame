﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoardGame.Domain.Enums;
using BoardGame.Domain.Logger;
using log4net;

namespace BoardGame.Client.Connect4.WPF
{
    public class Log4netAdapter : ILogger
    {
        private readonly ILog log;

        public Log4netAdapter(string loggerName)
        {
            log = LogManager.GetLogger(loggerName);
        }

        public void Log(LogEntry entry)
        {
            switch (entry.Severity)
            {
                case LoggingEventType.Debug:
                    log.Debug(entry.Message, entry.Exception);
                    break;
                case LoggingEventType.Info:
                    log.Info(entry.Message, entry.Exception);
                    break;
                case LoggingEventType.Warning:
                    log.Warn(entry.Message, entry.Exception);
                    break;
                case LoggingEventType.Error:
                    log.Error(entry.Message, entry.Exception);
                    break;
                case LoggingEventType.Fatal:
                    log.Fatal(entry.Message, entry.Exception);
                    break;
            }
        }
    }
}

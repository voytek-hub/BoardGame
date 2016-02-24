﻿using BoardGame.API;
using BoardGame.Client.Connect4.Mobile.NinjectModules;
using BoardGame.Client.Connect4.Mobile.Views;
using Ninject;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BoardGame.Client.Connect4.Mobile
{
    public class App : Application
    {
        public App()
        {
            MainPage = new StartPage();
        }

        protected override void OnStart()
        {
            IKernel kernel = new StandardKernel();
            var modules = new List<INinjectModule>
                {
                    new GameModule()
                };
            kernel.Load(modules);

            GameAPI api = kernel.Get<GameAPI>();
            api.MoveReceived += (s, e) => Debug.WriteLine("Received");
            Debug.WriteLine("Module loaded");
            Wait();
            Debug.WriteLine("Connecting...");
            api.StartGame(Domain.Enums.GameType.Online);
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        public async void Wait()
        {
            await Task.Delay(2000);
        }
    }
}

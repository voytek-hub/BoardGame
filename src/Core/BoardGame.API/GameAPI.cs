﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using BoardGame.Domain.Enums;
using BoardGame.Domain.Interfaces;
using BoardGame.Domain.Factories;
using BoardGame.Contracts;
using BoardGame.Contracts.Responses;
using BoardGame.Domain.Logger;
using System.ServiceModel;
using System.Threading.Tasks;
using BoardGame.API.Exceptions;

namespace BoardGame.API
{
    public class GameAPI : IGameAPI
    {
        private IGame CurrentGame { get; set; }
        private readonly IGameFactory gameFactory;
        private readonly IPlayerFactory playerFactory;
        private readonly IGameService proxy;
        private readonly ILogger logger;

        public event EventHandler<MoveEventArgs> OnMoveReceived;

        public GameAPI(IGameFactory gameFactory, 
                       IPlayerFactory playerFactory,
                       IGameService proxy = null,
                       ILogger logger = null)
        {
            Requires.IsNotNull(gameFactory, "gameFactory");
            Requires.IsNotNull(playerFactory, "playerFactory");

            this.gameFactory = gameFactory;
            this.playerFactory = playerFactory;
            this.proxy = proxy;
            this.logger = logger;
        }

        private void SendMove(IMove move)
        {
            Requires.IsNotNull(OnMoveReceived, "OnMoveReceived");

            OnMoveReceived?.Invoke(this, new MoveEventArgs { Move = move });
        }

        public async void StartGame(GameType type, string level = "")
        {
            try
            { 
                if (OnMoveReceived == null)
                {
                    logger?.Error("InvalidOperationException: " + StringResources.TheGameCanNotBeStartedBecauseOfOnMoveReceivedIsNull());
                    throw new InvalidOperationException(
                        StringResources.TheGameCanNotBeStartedBecauseOfOnMoveReceivedIsNull());
                }

                var players = new List<IPlayer>();
                switch (type)
                {
                    case GameType.SinglePlayer:
                        players.Add(playerFactory.Create(PlayerType.Human, 1));
                        players.Add(playerFactory.Create(PlayerType.Bot, 2));
                        break;
                    case GameType.TwoPlayers:
                        players.Add(playerFactory.Create(PlayerType.Human, 0));
                        players.Add(playerFactory.Create(PlayerType.Human, 0));
                        break;
                    case GameType.Online:
                        int myId = 0;

                        if (proxy == null)
                        {
                            logger?.Error("InvalidOperationException: " +
                                          StringResources.TheGameCanNotBeStartedBecauseOfProxyIsNull());
                            throw new InvalidOperationException(
                                StringResources.TheGameCanNotBeStartedBecauseOfProxyIsNull());
                        }

                        OnlineGameResponse waitingResponse = new OnlineGameResponse();
                        while (!waitingResponse.IsReady)
                        {
                            waitingResponse = await proxy.OnlineGameRequest(myId);
                            myId = waitingResponse.PlayerId;
                            if (waitingResponse.IsReady)
                            {
                                StartGameResponse startGameResponse =
                                    await proxy.ConfirmToPlay(waitingResponse.PlayerId);
                                if (startGameResponse.IsConfirmed)
                                {
                                    if (startGameResponse.YourTurn)
                                    {
                                        players.Add(playerFactory.Create(PlayerType.Human, waitingResponse.PlayerId));
                                        players.Add(playerFactory.Create(PlayerType.OnlinePlayer, 0));
                                    }
                                    else
                                    {
                                        players.Add(playerFactory.Create(PlayerType.OnlinePlayer, 0));
                                        players.Add(playerFactory.Create(PlayerType.Human, waitingResponse.PlayerId));
                                        GetFirstMove(waitingResponse.PlayerId);
                                    }
                                }
                                else
                                {
                                    waitingResponse.IsReady = false;
                                }
                            }
                        }
                        break;
                    case GameType.Bluetooth:
                        break;
                    case GameType.Wifi:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
                CurrentGame = gameFactory.Create(players, level);
            }
            catch (TimeoutException ex)
            {
                string exceptionMessage = StringResources.TimeoutExceptionOccured("StartGame", ex.Message);
                logger?.Error(exceptionMessage);

                throw new GameServerException(exceptionMessage, ex);
            }
            catch (FaultException ex)
            {
                string exceptionMessage = StringResources.ExceptionOccuredOnServerSide("StartGame", ex.Message);
                logger?.Error(exceptionMessage);

                throw new GameServerException(exceptionMessage, ex);
            }
            catch (CommunicationException ex)
            {
                string exceptionMessage = StringResources.CommunicationProblemOccured("StartGame", ex.Message);
                logger?.Error(exceptionMessage);

                throw new GameServerException(exceptionMessage, ex);
            }
        }

        private async void GetFirstMove(int playerId)
        {
            try
            {
                MoveResponse moveResponse = await proxy.GetFirstMove(playerId);
                if (moveResponse?.MoveMade != null)
                {
                    CurrentGame.MakeMove(moveResponse.MoveMade);
                    SendMove(moveResponse.MoveMade);
                }
            }
            catch (TimeoutException ex)
            {
                string exceptionMessage = StringResources.TimeoutExceptionOccured("GetFirstMove", ex.Message);
                logger?.Error(exceptionMessage);

                throw new GameServerException(exceptionMessage, ex);
            }
            catch (FaultException ex)
            {
                string exceptionMessage = StringResources.ExceptionOccuredOnServerSide("GetFirstMove", ex.Message);
                logger?.Error(exceptionMessage);

                throw new GameServerException(exceptionMessage, ex);
            }
            catch (CommunicationException ex)
            {
                string exceptionMessage = StringResources.CommunicationProblemOccured("GetFirstMove", ex.Message);
                logger?.Error(exceptionMessage);

                throw new GameServerException(exceptionMessage, ex);
            }
        }

        public async void NextMove(int clickedRow, int clickedColumn)
        {
            try
            {
                if (CurrentGame == null)
                {
                    logger?.Error("InvalidOperationException: " + StringResources.CanNotPerformTheMoveBecauseGameIsNull());
                    throw new InvalidOperationException(
                        StringResources.CanNotPerformTheMoveBecauseGameIsNull());
                }

                if (CurrentGame.IsMoveValid(-1, clickedColumn) && 
                    (CurrentGame.State != GameState.Finished || CurrentGame.State != GameState.Aborted))
                {
                    SendMove(CurrentGame.MakeMove(0, clickedColumn));

                    if (CurrentGame.NextPlayer == null)
                    {
                        logger?.Error("InvalidOperationException: " +
                                      StringResources.CanNotPerformNextMoveBecauseNextPlayerIsNull());
                        throw new InvalidOperationException(
                            StringResources.CanNotPerformNextMoveBecauseNextPlayerIsNull());
                    }

                    if (CurrentGame.State.Equals(GameState.Running) &&
                        CurrentGame.NextPlayer.Type.Equals(PlayerType.Bot))
                    {
                        if (CurrentGame.Bot == null)
                        {
                            logger?.Error("InvalidOperationException: " +
                                          StringResources.CanNotPerformBotsMoveBecauseBotWasNotDefined());
                            throw new InvalidOperationException(
                                StringResources.CanNotPerformBotsMoveBecauseBotWasNotDefined());
                        }

                        await Task.Run(() => CurrentGame.Bot.GenerateMove(CurrentGame))
                            .ContinueWith(task => SendMove(task.Result), TaskScheduler.FromCurrentSynchronizationContext());
                    }
                    else if (CurrentGame.NextPlayer.Type.Equals(PlayerType.OnlinePlayer))
                    {
                        if (proxy == null)
                        {
                            logger?.Error("InvalidOperationException: " +
                                          StringResources.CanNotPerformOnlineMoveBecauseOfProxyIsNull());
                            throw new InvalidOperationException(
                                StringResources.CanNotPerformOnlineMoveBecauseOfProxyIsNull());
                        }

                        int myId = CurrentGame.Players.First(p => p.Type.Equals(PlayerType.Human)).OnlineId;
                        MoveResponse moveResponse = await proxy.MakeMove(myId, 0, clickedColumn);
                        if (moveResponse?.MoveMade != null)
                        {
                            CurrentGame.MakeMove(moveResponse.MoveMade);
                            SendMove(moveResponse.MoveMade);
                        }
                    }
                }
            }
            catch (TimeoutException ex)
            {
                string exceptionMessage = StringResources.TimeoutExceptionOccured("NextMove", ex.Message);
                logger?.Error(exceptionMessage);

                throw new GameServerException(exceptionMessage, ex);
            }
            catch (FaultException ex)
            {
                string exceptionMessage = StringResources.ExceptionOccuredOnServerSide("NextMove", ex.Message);
                logger?.Error(exceptionMessage);

                throw new GameServerException(exceptionMessage, ex);
            }
            catch (CommunicationException ex)
            {
                string exceptionMessage = StringResources.CommunicationProblemOccured("NextMove", ex.Message);
                logger?.Error(exceptionMessage);

                throw new GameServerException(exceptionMessage, ex);
            }
        }

        public void Close()
        {
            logger?.Info("Closing GameAPI. Server connection will be aborted");
            ((ICommunicationObject) proxy).Abort();
        }
    }
}

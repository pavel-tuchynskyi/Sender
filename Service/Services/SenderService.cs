﻿using Domain.Interfaces.Services;
using Domain.Models.Configuration;
using Domain.Models.Message;
using Domain.Models.MessageTemplates;
using Domain.Models.Response;
using Domain.Models.Rules.EffectModels;
using LanguageExt.Common;
using Microsoft.Extensions.Options;
using Serilog;
using Service.Helpers.Services.ChannelsStrategies;
using Service.Helpers.Services.MessagesStrategies;

namespace Service.Services
{
    public class SenderService : ISenderService
    {
        private ChannelsConfiguration _channelsConfiguration;
        private MessageStrategyResolver _messageResolver;
        private ChannelStrategyResolver _channelResolver;

        public SenderService(IOptionsSnapshot<ChannelsConfiguration> channelsConfigurations)
        {
            _channelsConfiguration = channelsConfigurations.Value;
            _messageResolver = new MessageStrategyResolver();
            _channelResolver = new ChannelStrategyResolver(_channelsConfiguration);
        }

        public async Task<Result<bool>> SendRangeAsync<T>(List<T> objects, List<Effect> effects, Templates templates)
        {
            if (effects == null)
            {
                Log.Error("Effects is null");
                var ex = new ResponseException("Effects is null");
                return new Result<bool>(ex);
            }

            Log.Debug("Sending objects started.");

            Result<bool> res = false;
            foreach(var effect in effects)
            {
                var messageFactory = _messageResolver.GetMessageStrategy(effect, templates);
                var messages = messageFactory.CreateMessages(objects);

                var channel = _channelResolver.GetChannel(effect.Type);

                foreach(var message in messages)
                {
                    res = await channel.SendAsync(message);
                }
            }

            Log.Debug("Sending objects completed with result: {res}", res);
            return res;
        }

        public async Task<Result<bool>> TelegramSpamToUser(string phone, int messageCount, string message)
        {
            var telSender = new TelegramChannelStrategy(new ChannelsConfiguration
            {
                TelegramConfiguration = new TelegramConfiguration
                {
                    Api_Id = _channelsConfiguration.TelegramConfiguration.Api_Id,
                    Api_Hash = _channelsConfiguration.TelegramConfiguration.Api_Hash,
                    Phone = _channelsConfiguration.TelegramConfiguration.Phone,
                    Recepient_Phone = phone
                }
            });

            var telMsg = new TelegramMessage { Body = message };
            var res = await telSender.SendManyAsync(telMsg, messageCount);
            return res;
        }
    }
}

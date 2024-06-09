﻿using CheckMade.Common.Interfaces.Persistence.Tlg;
using CheckMade.Common.Model.Tlg.Input;
using CheckMade.Telegram.Model.BotCommand;
using CheckMade.Telegram.Model.DTOs;

namespace CheckMade.Telegram.Logic.UpdateProcessors.Concrete;

public interface ICommunicationsUpdateProcessor : IUpdateProcessor; 

public class CommunicationsUpdateProcessor(ITlgUpdateRepository updateRepo) : ICommunicationsUpdateProcessor
{
    public async Task<IReadOnlyList<OutputDto>> ProcessUpdateAsync(Result<TlgUpdate> tlgUpdate)
    {
        return await tlgUpdate.Match(
            async successfulUpdate =>
            {
                await updateRepo.AddAsync(successfulUpdate);

                if (successfulUpdate.Details.BotCommandEnumCode.GetValueOrDefault() == TlgStart.CommandCode)
                {
                    return new List<OutputDto>
                    {
                        new()
                        {
                            Text = UiConcatenate(
                                Ui("Welcome to the CheckMade {0} Bot! ", TlgBotType.Communications), 
                                IUpdateProcessor.SeeValidBotCommandsInstruction)
                        }
                    };
                }
                
                return new[] { new OutputDto() };
            },

            error => Task.FromResult<IReadOnlyList<OutputDto>>([ new OutputDto { Text = error } ])
        );
    }
}
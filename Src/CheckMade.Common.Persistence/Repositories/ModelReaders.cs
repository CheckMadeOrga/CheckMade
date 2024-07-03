using System.Data.Common;
using CheckMade.Common.Model.ChatBot;
using CheckMade.Common.Model.ChatBot.Input;
using CheckMade.Common.Model.ChatBot.UserInteraction;
using CheckMade.Common.Model.Core;
using CheckMade.Common.Model.Core.Actors;
using CheckMade.Common.Model.Core.Interfaces;
using CheckMade.Common.Model.Core.LiveEvents;
using CheckMade.Common.Model.Core.LiveEvents.SphereOfActionDetails;
using CheckMade.Common.Model.Core.Structs;
using CheckMade.Common.Model.Core.Trades;
using CheckMade.Common.Model.Core.Trades.Types;
using CheckMade.Common.Model.Utils;
using CheckMade.Common.Persistence.JsonHelpers;
using CheckMade.Common.Utils.Generic;

namespace CheckMade.Common.Persistence.Repositories;

internal static class ModelReaders
{
    internal static readonly Func<DbDataReader, Role> ReadRole = reader =>
    {
        var userInfo = ConstituteUserInfo(reader);
        var liveEventInfo = ConstituteLiveEventInfo(reader);

        return ConstituteRole(reader, userInfo, liveEventInfo.GetValueOrThrow());
    };

    internal static readonly Func<DbDataReader, TlgInput> ReadTlgInput = reader =>
    {
        var originatorRoleInfo = ConstituteRoleInfo(reader);
        var liveEventInfo = ConstituteLiveEventInfo(reader);
        
        return ConstituteTlgInput(reader, originatorRoleInfo, liveEventInfo);
    };

    internal static readonly Func<DbDataReader, TlgAgentRoleBind> ReadTlgAgentRoleBind = reader =>
    {
        var role = ReadRole(reader);
        var tlgAgent = ConstituteTlgAgent(reader);

        return ConstituteTlgAgentRoleBind(reader, role, tlgAgent);
    };
    
    internal static (
        Func<DbDataReader, int> getKey,
        Func<DbDataReader, User> initializeModel,
        Action<User, DbDataReader> accumulateData,
        Func<User, User> finalizeModel) GetUserReader()
    {
        return (
            getKey: reader => reader.GetInt32(reader.GetOrdinal("user_id")),
            initializeModel: reader => new User(ConstituteUserInfo(reader), new HashSet<IRoleInfo>()),
            accumulateData: (user, reader) =>
            {
                var roleInfo = ConstituteRoleInfo(reader);
                if (roleInfo.IsSome)
                    ((HashSet<IRoleInfo>)user.HasRoles).Add(roleInfo.GetValueOrThrow());
            },
            finalizeModel: user => user with { HasRoles = user.HasRoles.ToImmutableReadOnlyCollection() }
        );
    }
    
    internal static (
        Func<DbDataReader, int> getKey,
        Func<DbDataReader, LiveEvent> initializeModel,
        Action<LiveEvent, DbDataReader> accumulateData,
        Func<LiveEvent, LiveEvent> finalizeModel) GetLiveEventReader()
    {
        return (
            getKey: reader => reader.GetInt32(reader.GetOrdinal("live_event_id")),
            initializeModel: reader => 
                new LiveEvent(
                    ConstituteLiveEventInfo(reader).GetValueOrThrow(),
                    new HashSet<IRoleInfo>(),
                    ConstituteLiveEventVenue(reader),
                    new HashSet<ISphereOfAction>()),
            accumulateData: (liveEvent, reader) =>
            {
                var roleInfo = ConstituteRoleInfo(reader);
                if (roleInfo.IsSome)
                    ((HashSet<IRoleInfo>)liveEvent.WithRoles).Add(roleInfo.GetValueOrThrow());

                var sphereOfAction = ConstituteSphereOfAction(reader);
                if (sphereOfAction.IsSome)
                    ((HashSet<ISphereOfAction>)liveEvent.DivIntoSpheres).Add(sphereOfAction.GetValueOrThrow());
            },
            finalizeModel: liveEvent => liveEvent with
            {
                WithRoles = liveEvent.WithRoles.ToImmutableReadOnlyCollection(),
                DivIntoSpheres = liveEvent.DivIntoSpheres.ToImmutableReadOnlyCollection()
            }
        );
    }

    private static IUserInfo ConstituteUserInfo(DbDataReader reader)
    {
        return new UserInfo(
            new MobileNumber(reader.GetString(reader.GetOrdinal("user_mobile"))),
            reader.GetString(reader.GetOrdinal("user_first_name")),
            GetOption<string>(reader, reader.GetOrdinal("user_middle_name")),
            reader.GetString(reader.GetOrdinal("user_last_name")),
            GetOption<EmailAddress>(reader, reader.GetOrdinal("user_email")),
            EnsureEnumValidityOrThrow(
                (LanguageCode)reader.GetInt16(reader.GetOrdinal("user_language"))),
            EnsureEnumValidityOrThrow(
                (DbRecordStatus)reader.GetInt16(reader.GetOrdinal("user_status"))));
    }

    private static LiveEventVenue ConstituteLiveEventVenue(DbDataReader reader)
    {
        return new LiveEventVenue(
            reader.GetString(reader.GetOrdinal("venue_name")),
            EnsureEnumValidityOrThrow(
                (DbRecordStatus)reader.GetInt16(reader.GetOrdinal("venue_status"))));
    }

    private static Option<ILiveEventInfo> ConstituteLiveEventInfo(DbDataReader reader)
    {
        if (reader.IsDBNull(reader.GetOrdinal("live_event_name")))
            return Option<ILiveEventInfo>.None();
        
        return new LiveEventInfo(
            reader.GetString(reader.GetOrdinal("live_event_name")),
            reader.GetDateTime(reader.GetOrdinal("live_event_start_date")),
            reader.GetDateTime(reader.GetOrdinal("live_event_end_date")),
            EnsureEnumValidityOrThrow(
                (DbRecordStatus)reader.GetInt16(reader.GetOrdinal("live_event_status"))));
    }

    private static Option<ISphereOfAction> ConstituteSphereOfAction(DbDataReader reader)
    {
        if (reader.IsDBNull(reader.GetOrdinal("sphere_name")))
            return Option<ISphereOfAction>.None();

        var trade = GetTradeType();

        const string invalidTradeTypeException = $"""
                                                  This is not an existing '{nameof(trade)}' or we forgot to
                                                  implement a new type in method '{nameof(ConstituteSphereOfAction)}' 
                                                  """;

        var detailsJson = reader.GetString(reader.GetOrdinal("sphere_details"));
        
        ISphereOfActionDetails details = trade.Name switch
        {
            nameof(TradeSanitaryOps) => 
                JsonHelper.DeserializeFromJsonStrict<SanitaryCampDetails>(detailsJson) 
                ?? throw new InvalidDataException($"Failed to deserialize '{nameof(SanitaryCampDetails)}'!"),
            nameof(TradeSiteCleaning) => 
                JsonHelper.DeserializeFromJsonStrict<SiteCleaningZoneDetails>(detailsJson) 
                ?? throw new InvalidDataException($"Failed to deserialize '{nameof(SiteCleaningZoneDetails)}'!"),
            _ => 
                throw new InvalidOperationException(invalidTradeTypeException)
        };
        
        var sphereName = reader.GetString(reader.GetOrdinal("sphere_name"));

        ISphereOfAction sphere = trade.Name switch
        {
            nameof(TradeSanitaryOps) => 
                new SphereOfAction<TradeSanitaryOps>(sphereName, details),
            nameof(TradeSiteCleaning) => 
                new SphereOfAction<TradeSiteCleaning>(sphereName, details),
            _ => 
                throw new InvalidOperationException(invalidTradeTypeException)
        };
        
        return Option<ISphereOfAction>.Some(sphere);

        Type GetTradeType()
        {
            var domainGlossary = new DomainGlossary();
            var tradeId = new CallbackId(reader.GetString(reader.GetOrdinal("sphere_trade")));
            var tradeType = domainGlossary.TermById[tradeId].TypeValue;

            if (tradeType is null || 
                !tradeType.IsAssignableTo(typeof(ITrade)))
            {
                throw new InvalidDataException($"The '{nameof(tradeType)}:' '{tradeType?.FullName}' of this sphere " +
                                               $"can't be determined.");
            }

            return tradeType;
        }
    }
    
    private static Role ConstituteRole(DbDataReader reader, IUserInfo userInfo, ILiveEventInfo liveEventInfo) =>
        new(ConstituteRoleInfo(reader).GetValueOrThrow(),
            userInfo,
            liveEventInfo);

    private static Option<IRoleInfo> ConstituteRoleInfo(DbDataReader reader)
    {
        if (reader.IsDBNull(reader.GetOrdinal("role_token")))
            return Option<IRoleInfo>.None();
        
        return new RoleInfo(
            reader.GetString(reader.GetOrdinal("role_token")),
            EnsureEnumValidityOrThrow(
                (RoleType)reader.GetInt16(reader.GetOrdinal("role_type"))),
            EnsureEnumValidityOrThrow(
                (DbRecordStatus)reader.GetInt16(reader.GetOrdinal("role_status"))));
    }

    private static TlgInput ConstituteTlgInput(
        DbDataReader reader, Option<IRoleInfo> roleInfo, Option<ILiveEventInfo> liveEventInfo)
    {
        TlgUserId tlgUserId = reader.GetInt64(reader.GetOrdinal("input_user_id"));
        TlgChatId tlgChatId = reader.GetInt64(reader.GetOrdinal("input_chat_id"));
        var interactionMode = EnsureEnumValidityOrThrow(
            (InteractionMode)reader.GetInt16(reader.GetOrdinal("input_mode")));
        var tlgInputType = EnsureEnumValidityOrThrow(
            (TlgInputType)reader.GetInt16(reader.GetOrdinal("input_type")));
        var tlgDetails = reader.GetString(reader.GetOrdinal("input_details"));

        return new TlgInput(
            new TlgAgent(tlgUserId, tlgChatId, interactionMode),
            tlgInputType,
            roleInfo,
            liveEventInfo,
            JsonHelper.DeserializeFromJsonStrict<TlgInputDetails>(tlgDetails)
            ?? throw new InvalidDataException($"Failed to deserialize '{nameof(TlgInputDetails)}'!"));
    }

    private static TlgAgent ConstituteTlgAgent(DbDataReader reader)
    {
        return new TlgAgent(
            reader.GetInt64(reader.GetOrdinal("tarb_tlg_user_id")),
            reader.GetInt64(reader.GetOrdinal("tarb_tlg_chat_id")),
            EnsureEnumValidityOrThrow(
                (InteractionMode)reader.GetInt16(reader.GetOrdinal("tarb_interaction_mode"))));
    }

    private static TlgAgentRoleBind ConstituteTlgAgentRoleBind(DbDataReader reader, Role role, TlgAgent tlgAgent)
    {
        var activationDate = reader.GetDateTime(reader.GetOrdinal("tarb_activation_date"));

        var deactivationDateOrdinal = reader.GetOrdinal("tarb_deactivation_date");

        var deactivationDate = !reader.IsDBNull(deactivationDateOrdinal)
            ? Option<DateTime>.Some(reader.GetDateTime(deactivationDateOrdinal))
            : Option<DateTime>.None();

        var status = EnsureEnumValidityOrThrow(
            (DbRecordStatus)reader.GetInt16(reader.GetOrdinal("tarb_status")));

        return new TlgAgentRoleBind(role, tlgAgent, activationDate, deactivationDate, status);
    }
    
    private static Option<T> GetOption<T>(DbDataReader reader, int ordinal)
    {
        var valueRaw = reader.GetValue(ordinal);

        if (typeof(T) == typeof(EmailAddress) && valueRaw != DBNull.Value)
        {
            return (Option<T>) (object) Option<EmailAddress>.Some(
                new EmailAddress(reader.GetFieldValue<string>(ordinal)));
        }
        
        return valueRaw != DBNull.Value
            ? Option<T>.Some(reader.GetFieldValue<T>(ordinal))
            : Option<T>.None();
    }

    private static TEnum EnsureEnumValidityOrThrow<TEnum>(TEnum uncheckedEnum) where TEnum : Enum
    {
        if (!EnumChecker.IsDefined(uncheckedEnum))
            throw new InvalidDataException($"The value {uncheckedEnum} for enum of type {typeof(TEnum)} is invalid. " + 
                                           $"Forgot to migrate data in db?");
        
        return uncheckedEnum;
    }

}
using System.Text.RegularExpressions;
using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Entities;
using OpenConquer.GameServer.Session;
using OpenConquer.GameServer.Session.Managers;
using OpenConquer.GameServer.Session.Objects;
using OpenConquer.Protocol.Packets;
using OpenConquer.Domain.Enums;
using OpenConquer.GameServer.Calculations.Interface;

namespace OpenConquer.GameServer.Handlers
{
    public class CreatePlayerPacketHandler(UserManager userMgr, WorldManager world, ICharacterService chars, IExperienceService experienceService, ILogger<CreatePlayerPacketHandler> log) : IPacketHandler<CreatePlayerPacket>
    {
        private static readonly Regex NameRegex = new("^[a-zA-Z0-9]{3,16}$", RegexOptions.Compiled);

        // Might play around with this later
        //private static readonly int[] AllowedModels = [1003, 1004, 2001, 2002];
        //private static readonly int[] AllowedJobs = [10, 20, 40, 50, 60, 100];

        // Need to figure out what these should be
        private const ushort DefaultMapId = 1;
        private const ushort DefaultSpawnX = 100;
        private const ushort DefaultSpawnY = 100;

        private readonly UserManager _userMgr = userMgr ?? throw new ArgumentNullException(nameof(userMgr));
        private readonly WorldManager _world = world ?? throw new ArgumentNullException(nameof(world));
        private readonly ICharacterService _chars = chars ?? throw new ArgumentNullException(nameof(chars));
        private readonly IExperienceService _experienceService = experienceService ?? throw new ArgumentNullException(nameof(experienceService));
        private readonly ILogger<CreatePlayerPacketHandler> _log = log ?? throw new ArgumentNullException(nameof(log));

        public async Task HandleAsync(CreatePlayerPacket packet, GameClientSession session, CancellationToken ct)
        {
            // 0 = Create, 1 = Back/Cancel
            byte subtype = (byte)(packet.Unknown1 & 0xFF);
            if (subtype == 1)
            {
                // Client pressed "Back"
                await session.DisconnectAsync(ct);
                return;
            }

            string name = packet.FirstName;
            if (!NameRegex.IsMatch(name))
            {
                await session.SendAsync(new TalkPacket(speaker: "SYSTEM", hearer: session.User.Name, words: "Invalid name!", emotion: string.Empty, color: 0xffffff, type: ChatType.MessageBox), ct);
                return;
            }

            //if (!AllowedModels.Contains(packet.Model))
            //{
            //    await session.SendAsync(new TalkPacket(
            //        speaker: "SYSTEM",
            //        hearer: session.User.Name,
            //        words: "Invalid model!",
            //        emotion: string.Empty,
            //        color: 0xffffff,
            //        type: ChatType.MessageBox), ct);
            //    return;
            //}

            //if (!AllowedJobs.Contains(packet.Job))
            //{
            //    await session.SendAsync(new TalkPacket(
            //        speaker: "SYSTEM",
            //        hearer: session.User.Name,
            //        words: "Invalid job!",
            //        emotion: string.Empty,
            //        color: 0xffffff,
            //        type: ChatType.MessageBox), ct);
            //    return;
            //}

            if (await _chars.NameExistsAsync(name, ct))
            {
                await session.SendAsync(new TalkPacket(speaker: "SYSTEM", hearer: session.User.Name, words: "Name is in use!", emotion: string.Empty, color: 0xffffff, type: ChatType.MessageBox), ct);
                return;
            }

            Character newChar = new()
            {
                Name = name,
                Profession = (byte)packet.Job,
                Mesh = (uint)packet.Model,
                Hair = (ushort)packet.Model,
                MapID = DefaultMapId,
                X = DefaultSpawnX,
                Y = DefaultSpawnY,
                Health = _experienceService.GetMaxHealth((byte)packet.Job, 1),
                Mana = _experienceService.GetMaxMana((byte)packet.Job, 1)
            };

            await _chars.CreateAsync(newChar, ct);
            _log.LogInformation("Created character {Name} (UID={UID})", newChar.Name, newChar.UID);

            _userMgr.NewRoles.Add(newChar.UID);

            session.Character = newChar;
            _userMgr.Login(session);
            GameCharacter gameChar = new(newChar, session, _experienceService);
            _world.AddPlayer(gameChar);

            await session.SendAsync(new TalkPacket(speaker: "SYSTEM", hearer: "ALLUSERS", words: "ANSWER_OK", emotion: string.Empty, color: 0xffffff, type: ChatType.Register), ct);

            session.User.Health = Math.Max(session.User.Health, 1);

            await gameChar.CompleteLoginAsync(ct);
        }
    }
}

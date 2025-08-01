using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Entities;
using OpenConquer.Domain.Enums;
using OpenConquer.GameServer.Calculations.Interface;
using OpenConquer.GameServer.Session;
using OpenConquer.GameServer.Session.Managers;
using OpenConquer.GameServer.Session.Objects;
using OpenConquer.Protocol.Packets;

namespace OpenConquer.GameServer.Handlers
{
    public class ConnectPacketHandler(ILogger<ConnectPacketHandler> log, UserManager userMgr, WorldManager worldManager, IAccountService accountService, ICharacterService characterService, IExperienceService experienceService) : IPacketHandler<ConnectPacket>
    {
        private readonly ILogger<ConnectPacketHandler> _log = log ?? throw new ArgumentNullException(nameof(log));
        private readonly UserManager _userMgr = userMgr ?? throw new ArgumentNullException(nameof(userMgr));
        private readonly WorldManager _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
        private readonly IAccountService _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        private readonly ICharacterService _characterService = characterService ?? throw new ArgumentNullException(nameof(characterService));
        private readonly IExperienceService _experienceService = experienceService ?? throw new ArgumentNullException(nameof(experienceService));

        public async Task HandleAsync(ConnectPacket packet, GameClientSession session, CancellationToken ct)
        {
            _log.LogInformation("Login request (hash={Hash})", packet.Data);

            uint uid = await _accountService.PullKeyAsync(packet.Data, ct);
            if (uid == 0)
            {
                _log.LogWarning("Bad credentials (hash={Hash})", packet.Data);
                await session.DisconnectAsync(ct);
                return;
            }

            if (_userMgr.IsOnline(uid))
            {
                GameClientSession oldSession = _userMgr.Get(uid);
                await oldSession.DisconnectAsync(ct);
                await session.DisconnectAsync(ct);
                return;
            }

            await session.SendAsync(Unknown2079Packet.Create(0), ct);
            await session.SendAsync(Unknown2078Packet.Create(0x4e591dba), ct);

            Character character = new() { UID = uid };
            session.Character = character;

            bool isExisting = await _characterService.PullLoginAsync(character, ct);

            if (!isExisting && _userMgr.NewRoles.Contains(uid))
            {
                _userMgr.NewRoles.Remove(uid);
                isExisting = true;
            }

            if (!isExisting)
            {
                await session.SendAsync(new TalkPacket(speaker: "SYSTEM", hearer: "ALLUSERS", words: "NEW_ROLE", emotion: string.Empty, color: 0xffffff, type: ChatType.Entrance), ct);
                return;
            }

            _userMgr.Login(session);
            GameCharacter gameChar = new(session.User, session, _experienceService);

            _worldManager.AddPlayer(gameChar);
            await session.SendAsync(new TalkPacket(speaker: "SYSTEM", hearer: "ALLUSERS", words: "ANSWER_OK", emotion: string.Empty, color: 0xffffff, type: ChatType.Entrance), ct);

            session.User.Health = Math.Max(session.User.Health, 1);

            await gameChar.CompleteLoginAsync(ct);
        }
    }
}

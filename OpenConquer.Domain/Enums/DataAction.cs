
namespace OpenConquer.Domain.Enums
{
    public enum DataAction : ushort
    {
        None = 0,
        EnterMap = 74,
        GetItemSet = 75,
        GetGoodFriend = 76,
        GetWeaponSkillSet = 77,
        GetMagicSet = 78,
        ChangeDirection = 79,
        ChangeAction = 81,

        /// <summary>
        /// [85]
        /// </summary>
        ChangeMap = 85,
        Teleport = 86,
        LevelUp = 92,
        XpClear = 93,
        Revive = 94,
        DelRole = 95,
        SetPkMode = 96,
        GetSynAttr = 97,

        /// <summary>
        /// [99]
        /// </summary>
        Mine = 99,

        /// <summary>
        /// [101]
        /// Data2 = TeamMemberId,
        /// Data3Low = PositionX,
        /// Data3High = PositionY
        /// </summary>
        TeamMemberPos = 101,
        QueryPlayer = 102,
        AbortMagic = 103,
        MapARGB = 104,
        MapStatus = 105,

        /// <summary>
        /// [106]
        /// Data3Low = PositionX,
        /// Data3High = PositionY
        /// </summary>
        QueryTeamMember = 106,
        Kickback = 108,
        DropMagic = 109,
        DropSkill = 110,

        /// <summary>
        /// [111]
        /// Data2 = BoothId,
        /// Data3Low = PositionX,
        /// Data3High = PositionY,
        /// Data4 = Direction
        /// </summary>
        CreateBooth = 111,
        SuspendBooth = 112,
        ResumeBooth = 113,
        GetSurroundings = 114,
        PostCmd = 116,

        /// <summary>
        /// [117]
        /// Data2 = TargetId
        /// </summary>
        QueryEquipment = 117,
        AbortTransform = 118,
        EndFly = 120,

        /// <summary>
        /// [121]
        /// Data2
        /// </summary>
        GetMoney = 121,
        OpenDialog = 126,
        GuardJump = 130,
        //Login = 132,

        /// <summary>
        /// [134] 
        /// Data1 = EntityId,
        /// Data3Low = PositionX,
        /// Data3High = PositionY
        /// </summary>
        SpawnEffect = 134,

        /// <summary>
        /// [135] 
        /// Data1 = EntityId
        /// </summary>
        RemoveEntity = 135,//was 132, it changed!

        /// <summary>
        /// [133]
        /// </summary>
        Jump = 137,

        /// <summary>
        /// [137]
        /// </summary>
        //Ghost = 137,//do not know what this is
        TeleportReply = 138,

        DieQuestion = 145,

        /// <summary>
        /// [148]
        /// Data1 = FriendId
        /// </summary>
        QueryFriendInfo = 148,
        ChangeFace = 151,
        ItemsDetained = 155,
        NinjaStep = 156,

        HideInterface = 158,

        OpenUpgrade = 160,

        /// <summary>
        /// [161] 
        /// Data1 = Mode (0=none,1=away)
        /// </summary>
        AwayFromKeyboard = 161,
        PathFinding = 162,
        DragonBallDropped = 165,

        TableState = 233,
        TablePot = 234,
        TablePlayerCount = 235,

        /// <summary>
        /// [310]
        /// Data2 = FriendId
        /// </summary>
        QueryFriendEquip = 310,

        QueryStatInfo = 408,
    }
}

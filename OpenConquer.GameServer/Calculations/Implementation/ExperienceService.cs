using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Enums;
using OpenConquer.GameServer.Calculations.Interface;

namespace OpenConquer.GameServer.Calculations.Implementation
{
    public class ExperienceService(ILevelStatService statService, ILogger<ExperienceService> logger) : IExperienceService, IHostedService
    {
        private readonly ILevelStatService _statService = statService ?? throw new ArgumentNullException(nameof(statService));
        private readonly ILogger<ExperienceService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private Dictionary<byte, ulong> _xpByLevel = [];
        private Dictionary<byte, uint> _xpByProficiency = [];
        private Dictionary<Profession, Dictionary<byte, LevelStats>> _statsByProfession = [];

        private static readonly ushort[] StonePoints = [1, 10, 40, 120, 360, 1_080, 3_240, 9_720, 29_160];
        private static readonly ushort[] ComposePoints = [20, 20, 80, 240, 720, 2_160, 6_480, 19_440, 58_320, 2_700, 5_500, 9_000, 0];
        private static readonly byte[] SteedSpeeds = [0, 5, 10, 15, 20, 30, 40, 50, 65, 85, 90, 95, 100];
        private static readonly ushort[] TalismanExtras = [0, 6, 30, 70, 240, 740, 2_240, 6_670, 20_000, 60_000];

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Domain.Entities.LevelStat> allStats = await _statService.GetAllAsync(cancellationToken);

            _statsByProfession = allStats.GroupBy(s => s.Profession)
                .ToDictionary(grp => grp.Key, grp => grp
                .ToDictionary(ls => ls.Level, ls => new LevelStats(ls.Strength, ls.Agility, ls.Vitality, ls.Spirit, ls.Health, ls.Mana)));

            IEnumerable<Domain.Entities.ExperienceCurve> curves = await _statService.GetAllCurvesAsync(cancellationToken);

            _xpByLevel = curves.Where(c => c.CurveType == "Level").ToDictionary(c => c.Level, c => c.Experience);
            _xpByProficiency = curves.Where(c => c.CurveType == "Proficiency").ToDictionary(c => c.Level, c => Convert.ToUInt32(c.Experience));

            _logger.LogInformation("ExperienceService initialized: loaded {StatCount} level‐stat entries across {ProfessionCount} professions, {CurveCount} curve entries", allStats.Count(), _statsByProfession.Count, curves.Count());
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ulong GetLevelExperience(byte level) => _xpByLevel.TryGetValue(level, out ulong xp) ? xp : 0UL;

        public uint GetProficiencyExperience(byte tier) => _xpByProficiency.TryGetValue(tier, out uint xp) ? xp : 0U;

        public int GetMaxHealth(byte profession, byte level) => _statsByProfession[(Profession)profession][level].Health;

        public int GetMaxMana(byte profession, byte level) => _statsByProfession[(Profession)profession][level].Mana;

        public ulong CalculateDamageExperience(int monsterMaxHealth, byte monsterLevel, byte playerLevel, uint damage)
        {
            double exp = Math.Min(monsterMaxHealth, (int)damage);
            int delta = playerLevel - monsterLevel;

            if (delta >= 3)
            {
                if (delta <= 5)
                {
                    exp *= 0.7;
                }
                else if (delta <= 10)
                {
                    exp *= 0.2;
                }
                else if (delta <= 20)
                {
                    exp *= 0.1;
                }
                else
                {
                    exp *= 0.05;
                }
            }
            else if (delta < 0)
            {
                if (delta >= -5)
                {
                    exp *= 1.3;
                }
                else if (delta >= -10)
                {
                    exp *= 1.5;
                }
                else if (delta >= -20)
                {
                    exp *= 1.8;
                }
                else
                {
                    exp *= 2.3;
                }
            }

            return (ulong)Math.Max(1, exp);
        }

        public uint CalculateKillBonusExperience(int monsterMaxHealth) => (uint)(monsterMaxHealth * 5 / 100);

        public uint StonePlusPoints(byte plus) => StonePoints[Math.Min(plus, (byte)(StonePoints.Length - 1))];

        public uint ComposePlusPoints(byte plus) => ComposePoints[Math.Min(plus, (byte)(ComposePoints.Length - 1))];

        public byte SteedSpeed(byte plus) => SteedSpeeds[Math.Min(plus, (byte)(SteedSpeeds.Length - 1))];

        public ushort TalismanPlusPoints(byte plus) => TalismanExtras[Math.Min(plus, (byte)(TalismanExtras.Length - 1))];
    }

    public record LevelStats(int Strength, int Agility, int Vitality, int Spirit, int Health, int Mana);
}

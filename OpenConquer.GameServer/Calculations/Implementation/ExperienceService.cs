using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Enums;
using OpenConquer.GameServer.Calculations.Interface;

namespace OpenConquer.GameServer.Calculations.Implementation
{
    public class ExperienceService(ILevelStatService statService) : IExperienceService, IHostedService
    {
        private readonly ILevelStatService _statService = statService;
        private static readonly uint[] _proficiencyExperience = [0, 1_200, 68_000, 250_000, 640_000, 1_600_000, 4_000_000, 10_000_000, 22_000_000, 40_000_000, 90_000_000, 95_000_000, 142_500_000, 213_750_000, 320_625_000, 480_937_500, 721_406_250, 1_082_109_375, 1_623_164_063, 2_100_000_000, 0];

        private static readonly ulong[] _levelExperience = [120, 180, 240, 360, 600, 960, 1_200, 2_400, 3_600, 8_400, 12_000, 14_400, 18_000, 21_600, 22_646, 32_203, 37_433, 47_556,
            56_609, 68_772, 70_515, 75_936, 97_733, 114_836, 120_853, 123_981, 126_720, 145_878, 173_436, 197_646, 202_451, 212_160, 244_190, 285_823, 305_986, 312_864, 324_480, 366_168, 433_959, 460_590,
            506_738, 569_994, 728_527, 850_829, 916_479, 935_118, 940_800, 1_076_593, 1_272_780, 1_357_994, 1_384_861, 1_478_400, 1_632_438, 1_903_104, 2_066_042, 2_104_924, 1_921_085, 2_417_202, 2_853_462, 3_054_574, 
            3_111_217, 3_225_600, 3_810_962, 4_437_896, 4_880_605, 4_970_962, 5_107_200, 5_652_518, 6_579_162, 6_877_991, 7_100_700, 7_157_657, 9_106_860, 10_596_398, 11_220_549, 11_409_192, 11_424_000, 12_882_952, 15_172_807,
            15_896_990, 16_163_799, 16_800_000, 19_230_280, 22_365_208, 23_819_312, 24_219_528, 24_864_000, 27_200_077, 32_033_165, 33_723_801, 34_291_317, 34_944_000, 39_463_523, 45_878_567, 48_924_236, 49_729_220, 51_072_000,
            55_808_379, 64_870_058, 68_391_931, 69_537_026, 76_422_968, 96_950_789, 112_676_755, 120_090_482, 121_798_280, 127_680_000, 137_446_887, 193_715_970, 408_832_150, 454_674_685, 461_125_885, 469_189_885, 477_253_885,
            480_479_485, 485_317_885, 493_381_885, 580_580_046, 717_424_987, 282_274_058, 338_728_870, 406_474_644, 487_769_572, 585_323_487, 702_388_184, 842_865_821, 1_011_438_985, 1_073_741_823, 1_073_741_823, 8_589_134_588,
            25_767_403_764, 77_302_211_292, 231_906_633_876, 347_859_950_814, 447_859_950_814, 547_859_950_814
        ];
        private Dictionary<Profession, Dictionary<byte, LevelStats>> _statsByProfession = [];

        private static readonly ushort[] StonePoints = [1, 10, 40, 120, 360, 1_080, 3_240, 9_720, 29_160];
        private static readonly ushort[] ComposePoints = [20, 20, 80, 240, 720, 2_160, 6_480, 19_440, 58_320, 2_700, 5_500, 9_000, 0];
        private static readonly byte[] SteedSpeeds = [0, 5, 10, 15, 20, 30, 40, 50, 65, 85, 90, 95, 100];
        private static readonly ushort[] TalismanExtras = [0, 6, 30, 70, 240, 740, 2_240, 6_670, 20_000, 60_000];

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Domain.Entities.LevelStat> all = await _statService.GetAllAsync(cancellationToken);
            _statsByProfession = all.GroupBy(s => s.Profession).ToDictionary(grp => grp.Key, grp => grp.ToDictionary(
                ls => ls.Level,
                ls => new LevelStats(ls.Strength, ls.Agility, ls.Vitality, ls.Spirit, ls.Health, ls.Mana))
            );
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ulong GetLevelExperience(byte level) => _levelExperience[Math.Clamp(level - 1, 0, _levelExperience.Length - 1)];

        public uint GetProficiencyExperience(byte level) => _proficiencyExperience[Math.Clamp(level, 0, _proficiencyExperience.Length - 1)];

        public int GetMaxHealth(byte profession, byte level) => _statsByProfession[(Profession)profession][level].Health;

        public int GetMaxMana(byte profession, byte level) => _statsByProfession[(Profession)profession][level].Mana;

        public ulong CalculateDamageExperience(int monsterMaxHealth, byte monsterLevel, byte playerLevel, uint damage)
        {
            double exp = Math.Min(monsterMaxHealth, (int)damage);

            int deltaLevel = playerLevel - monsterLevel;

            if (deltaLevel >= 3) // Green or higher name mob
            {
                if (deltaLevel <= 5)
                {
                    exp *= 0.7;
                }
                else if (deltaLevel <= 10)
                {
                    exp *= 0.2;
                }
                else if (deltaLevel <= 20)
                {
                    exp *= 0.1;
                }
                else
                {
                    exp *= 0.05;
                }
            }
            else if (deltaLevel >= 0)
            {
                // white name mob no modifier 
            }
            else if (deltaLevel >= -5) // red name mob
            {
                exp += exp * 0.3;
            }
            else // black name mob
            {
                if (deltaLevel >= -10)
                {
                    exp += exp * 0.5;
                }
                else if (deltaLevel >= -20)
                {
                    exp += exp * 0.8;
                }
                else
                {
                    exp += exp * 1.3;
                }
            }

            // always at least 1 XP
            if (exp < 1)
            {
                exp = 1;
            }

            return (ulong)exp;
        }

        public uint CalculateKillBonusExperience(int monsterMaxHealth) => (uint)(monsterMaxHealth * 5 / 100);

        public uint StonePlusPoints(byte plus) => StonePoints[Math.Min(plus, (byte)(StonePoints.Length - 1))];

        public uint ComposePlusPoints(byte plus) => ComposePoints[Math.Min(plus, (byte)(ComposePoints.Length - 1))];

        public byte SteedSpeed(byte plus) => SteedSpeeds[Math.Min(plus, (byte)(SteedSpeeds.Length - 1))];

        public ushort TalismanPlusPoints(byte plus) => TalismanExtras[Math.Min(plus, (byte)(TalismanExtras.Length - 1))];
    }

    public record LevelStats(int Strength, int Agility, int Vitality, int Spirit, int Health, int Mana)
    {
        public static LevelStats Parse(string raw)
        {
            string[] parts = raw.Split(',', StringSplitOptions.TrimEntries);
            return new LevelStats(Strength: int.Parse(parts[0]), Agility: int.Parse(parts[1]), Vitality: int.Parse(parts[2]), Spirit: int.Parse(parts[3]), Health: int.Parse(parts[4]), Mana: int.Parse(parts[5]));
        }
    }
}

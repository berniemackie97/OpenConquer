
namespace OpenConquer.GameServer.Calculations.Interface
{
    public interface IExperienceService
    {
        ulong GetLevelExperience(byte level);
        uint GetProficiencyExperience(byte level);
        int GetMaxHealth(byte profession, byte level);
        int GetMaxMana(byte profession, byte level);
        ulong CalculateDamageExperience(int monsterMaxHealth, byte monsterLevel, byte playerLevel, uint damage);
        uint CalculateKillBonusExperience(int monsterMaxHealth);
        uint StonePlusPoints(byte plus);
        uint ComposePlusPoints(byte plus);
        byte SteedSpeed(byte plus);
        ushort TalismanPlusPoints(byte plus);
    }
}

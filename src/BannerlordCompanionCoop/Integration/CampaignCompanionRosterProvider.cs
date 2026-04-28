using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordCompanionCoop.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace BannerlordCompanionCoop.Integration;

public static class CampaignCompanionRosterProvider
{
    public static bool TryCreateMissionSeed(
        out string saveId,
        out IReadOnlyList<CompanionHeroProfile> companionProfiles)
    {
        saveId = "campaign_current";
        companionProfiles = Array.Empty<CompanionHeroProfile>();

        Campaign? campaign = Campaign.Current;
        if (campaign is null)
        {
            return false;
        }

        Clan? playerClan = Clan.PlayerClan;
        MobileParty? mainParty = MobileParty.MainParty;
        if (playerClan is null || mainParty is null)
        {
            return false;
        }

        List<CompanionHeroProfile> profiles = new();

        foreach (Hero hero in playerClan.Companions)
        {
            if (!IsEligibleMissionCompanion(hero, mainParty) || hero.CharacterObject is null)
            {
                continue;
            }

            string heroStringId = hero.StringId ?? string.Empty;
            string characterStringId = hero.CharacterObject.StringId ?? string.Empty;
            string displayName = hero.Name?.ToString() ?? hero.CharacterObject.Name?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(heroStringId)
                || string.IsNullOrWhiteSpace(characterStringId)
                || string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            profiles.Add(
                new CompanionHeroProfile(
                    heroStringId,
                    characterStringId,
                    displayName,
                    DeterminePreferredRole(hero),
                    hero.CharacterObject.Level,
                    hero.IsWounded));
        }

        if (profiles.Count == 0)
        {
            return false;
        }

        saveId = string.IsNullOrWhiteSpace(campaign.UniqueGameId)
            ? "campaign_current"
            : campaign.UniqueGameId;

        companionProfiles = profiles
            .OrderBy(profile => profile.DisplayName, StringComparer.Ordinal)
            .ToArray();

        return true;
    }

    private static bool IsEligibleMissionCompanion(Hero hero, MobileParty mainParty)
    {
        return hero.IsPlayerCompanion
            && hero.IsActive
            && hero.IsAlive
            && !hero.IsDead
            && !hero.IsDisabled
            && !hero.IsPrisoner
            && hero.PartyBelongedTo == mainParty;
    }

    private static CompanionMissionRole DeterminePreferredRole(Hero hero)
    {
        CompanionMissionRole selectedRole = CompanionMissionRole.Fighter;
        int bestScore = GetFighterScore(hero);

        UpdateRoleIfHigherScore(hero, DefaultSkills.Scouting, CompanionMissionRole.Scout, ref selectedRole, ref bestScore);
        UpdateRoleIfHigherScore(hero, DefaultSkills.Medicine, CompanionMissionRole.Surgeon, ref selectedRole, ref bestScore);
        UpdateRoleIfHigherScore(hero, DefaultSkills.Engineering, CompanionMissionRole.Engineer, ref selectedRole, ref bestScore);
        UpdateRoleIfHigherScore(hero, DefaultSkills.Steward, CompanionMissionRole.Quartermaster, ref selectedRole, ref bestScore);

        return selectedRole;
    }

    private static int GetFighterScore(Hero hero)
    {
        return Max(
            GetSkillValue(hero, DefaultSkills.OneHanded),
            GetSkillValue(hero, DefaultSkills.TwoHanded),
            GetSkillValue(hero, DefaultSkills.Polearm),
            GetSkillValue(hero, DefaultSkills.Bow),
            GetSkillValue(hero, DefaultSkills.Crossbow),
            GetSkillValue(hero, DefaultSkills.Throwing),
            GetSkillValue(hero, DefaultSkills.Athletics),
            GetSkillValue(hero, DefaultSkills.Riding));
    }

    private static void UpdateRoleIfHigherScore(
        Hero hero,
        SkillObject? skill,
        CompanionMissionRole role,
        ref CompanionMissionRole selectedRole,
        ref int bestScore)
    {
        int score = GetSkillValue(hero, skill);
        if (score <= bestScore)
        {
            return;
        }

        bestScore = score;
        selectedRole = role;
    }

    private static int GetSkillValue(Hero hero, SkillObject? skill)
    {
        return skill is null ? 0 : hero.GetSkillValue(skill);
    }

    private static int Max(params int[] values)
    {
        int max = 0;

        foreach (int value in values)
        {
            if (value > max)
            {
                max = value;
            }
        }

        return max;
    }
}

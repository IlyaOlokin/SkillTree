// Counterfactual stat values are supplied by production StatCalculator.
// These deterministic metrics approximate an encounter, not a combat replay.
double[] DecisionMetrics(System.Collections.Generic.Dictionary<StatType,float> stats,
    System.Collections.Generic.Dictionary<StatType,float> opponent) {
    double S(StatType stat) => stats.TryGetValue(stat, out var value) ? value : 0;
    double E(StatType stat) => opponent.TryGetValue(stat, out var value) ? value : 0;
    double Positive(double value) => System.Math.Max(0, value);
    double Clamp(double value, double max = 1) => System.Math.Max(0, System.Math.Min(max, value));
    double Hit(double accuracy, double evasion) => evasion <= 0 ? 1 : System.Math.Max(0.01, Clamp(0.8 * (Positive(accuracy) + 5) / evasion));
    double Resistance(double value, double maximum, double penetration) {
        double capped = System.Math.Min(value, maximum);
        return capped > 0 ? Positive(capped - Positive(penetration)) : capped;
    }
    var damageStats = new[] { StatType.PhysicalDamage, StatType.FireDamage, StatType.ColdDamage, StatType.LightningDamage, StatType.LightDamage, StatType.DarknessDamage };
    var mitigations = new[] { StatType.PhysicalDamageMitigation, StatType.FireDamageMitigation, StatType.ColdDamageMitigation, StatType.LightningDamageMitigation, StatType.LightDamageMitigation, StatType.DarknessDamageMitigation };
    var specific = new[] { StatType.FireResistance, StatType.ColdResistance, StatType.LightningResistance };
    var maxima = new[] { StatType.MaxFireResistance, StatType.MaxColdResistance, StatType.MaxLightningResistance };
    var penetrations = new[] { StatType.FireResistancePenetration, StatType.ColdResistancePenetration, StatType.LightningResistancePenetration };
    double[] Components(bool outgoing) {
        System.Func<StatType,double> attacker = outgoing ? (System.Func<StatType,double>)S : E;
        System.Func<StatType,double> defender = outgoing ? (System.Func<StatType,double>)E : S;
        var result = damageStats.Select(stat => Positive(attacker(stat)) * Positive(1 + Positive(attacker(StatType.CritChance)) * attacker(StatType.CritDamageBonus))).ToArray();
        if (result[0] > 0) result[0] *= result[0] / (Positive(defender(StatType.Armor)) + result[0]);
        for (int i = 1; i <= 3; i++) result[i] *=
            (1 - Resistance(defender(StatType.ElementalResistance), defender(StatType.MaxElementalResistance), attacker(StatType.ElementalResistancePenetration))) *
            (1 - Resistance(defender(specific[i - 1]), defender(maxima[i - 1]), attacker(penetrations[i - 1])));
        double net = result[4] - result[5];
        double negation = (Positive(defender(StatType.MaximumHealth)) + Positive(defender(StatType.BarrierCapacity))) * Positive(defender(StatType.MysticNegation));
        // Production only cancels opposing mystic types in the negation stage when negation is positive.
        if (negation > 0 && net != 0) {
            result[4] = net > 0 ? Positive(net - negation) : 0;
            result[5] = net < 0 ? Positive(-net - negation) : 0;
        }
        for (int i = 0; i < result.Length; i++) result[i] *= 1 - Clamp(defender(mitigations[i]));
        return result;
    }
    var dealt = Components(true); var taken = Components(false);
    double enemyHealth = System.Math.Max(1, E(StatType.MaximumHealth)), health = System.Math.Max(1, S(StatType.MaximumHealth));
    double block = Clamp(S(StatType.BlockChance), 0.9), parry = Clamp(S(StatType.ParryChance));
    // Parry still advances attacks on a zero-power block; it is not gated by BlockPower.
    double speed = Positive(S(StatType.AttackSpeed)) * (1 + block * parry * 0.3 * Positive(1 + S(StatType.ParryPower)));
    double direct = dealt.Take(4).Sum() + System.Math.Abs(dealt[4] - dealt[5]);
    double dot = 0.3 * dealt[1] * Clamp(dealt[1] / enemyHealth * Positive(1 + S(StatType.IgniteChance))) * Positive(1 + S(StatType.IgnitePower)) +
        0.3 * dealt[0] * Clamp(dealt[0] / enemyHealth * Positive(1 + S(StatType.BleedChance))) * Positive(1 + S(StatType.BleedPower));
    // A bounded proxy for non-damage ailments/debuffs; unavailable damage families contribute zero.
    double control = (dealt[2] > 0 ? 0.1 * Clamp(dealt[2] / enemyHealth * Positive(1 + S(StatType.ChillChance))) * Positive(1 + S(StatType.ChillPower)) : 0) +
        (dealt[3] > 0 ? 0.1 * Clamp(dealt[3] / enemyHealth * Positive(1 + S(StatType.OverchargeChance))) * Positive(1 + S(StatType.OverchargePower)) : 0);
    foreach (var pair in new[] { new[] { StatType.SunderChance, StatType.SunderPower }, new[] { StatType.DistractChance, StatType.DistractPower }, new[] { StatType.ExposeChance, StatType.ExposePower } })
        control += 0.05 * Clamp(S(pair[0])) * Positive(1 + S(pair[1]));
    double offence = (direct + System.Math.Min(dot, direct)) * speed * Hit(S(StatType.Accuracy), E(StatType.Evasion)) * (1 + System.Math.Min(0.5, control));
    double incomingHit = taken.Sum(), blockPower = Positive(S(StatType.BlockPower));
    double absorption = block * ((1 - parry) * System.Math.Min(incomingHit, blockPower) + parry * System.Math.Min(incomingHit, 2 * blockPower));
    double blockMultiplier = incomingHit > 0 ? Positive(incomingHit - absorption) / incomingHit : 1;
    double remainingHit = (taken.Take(4).Sum() + System.Math.Abs(taken[4] - taken[5])) * blockMultiplier;
    int barrierCount = (int)Positive(S(StatType.BarrierCount));
    int mask = (int)S(StatType.BarrierDamageTypeMask);
    double eligibleHit = 0;
    for (int i = 0; i < damageStats.Length; i++) {
        var type = (Battle.DamageType)System.Enum.Parse(typeof(Battle.DamageType), damageStats[i].ToString().Replace("Damage", ""));
        if ((mask & (int)type) != 0) eligibleHit += taken[i];
    }
    double barrierPerCharge = barrierCount > 0 ? System.Math.Min(eligibleHit, System.Math.Max(1, S(StatType.BarrierCapacity))) : 0;
    double enemySpeed = Positive(E(StatType.AttackSpeed)) * Hit(E(StatType.Accuracy), S(StatType.Evasion));
    double sustainedBarrier = barrierCount > 0 ? barrierPerCharge * Positive(S(StatType.BarrierRegenerationSpeed)) / 4 : 0;
    int stealMask = (int)S(StatType.LifeStealTypeMask); double stealDamage = 0;
    for (int i = 0; i < damageStats.Length; i++) {
        var type = (Battle.DamageType)System.Enum.Parse(typeof(Battle.DamageType), damageStats[i].ToString().Replace("Damage", ""));
        if ((stealMask & (int)type) != 0) stealDamage += dealt[i];
    }
    double healing = (Positive(S(StatType.HealthRegenerationPerSecond)) + stealDamage * speed * Hit(S(StatType.Accuracy), E(StatType.Evasion)) * Positive(S(StatType.LifeSteal))) * Positive(1 + S(StatType.HealingReceived));
    // Cleanse has value only against uncancelled mystic pressure. Healing cannot cleanse it.
    double mysticPressure = System.Math.Abs(taken[4] - taken[5]) * blockMultiplier * enemySpeed;
    double cleanse = System.Math.Min(mysticPressure, Positive(S(StatType.MysticCleansePerSecond)) * health);
    double incoming = remainingHit * enemySpeed;
    healing = System.Math.Min(healing, taken.Take(4).Sum() * blockMultiplier * enemySpeed);
    double recovery = System.Math.Min(incoming * 0.75, healing + cleanse + sustainedBarrier);
    double defence = (health + barrierPerCharge * barrierCount) / System.Math.Max(1, incoming - recovery);
    // Ailment/debuff mitigation saturates; these are conservative secondary proxies.
    double protection = 0;
    var guards = new[] { StatType.BleedMitigation, StatType.IgniteMitigation, StatType.ChillDurationReduction, StatType.OverchargeAvoidanceChance };
    for (int i = 0; i < guards.Length; i++) if (taken[i] > 0) protection += 0.025 * Clamp(S(guards[i]));
    foreach (var pair in new[] { new[] { StatType.SunderChance, StatType.SunderMitigation }, new[] { StatType.DistractChance, StatType.DistractMitigation }, new[] { StatType.ExposeChance, StatType.ExposeMitigation } })
        protection += 0.025 * Clamp(E(pair[0])) * Clamp(S(pair[1]));
    return new[] { System.Math.Max(0.000001, offence), System.Math.Max(0.000001, defence * (1 + protection)) };
}

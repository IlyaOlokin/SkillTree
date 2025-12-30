using System;
using System.Collections.Generic;
using Battle;
using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private List<Transform> enemySpawnPositions;
    [SerializeField] private EnemyUnit enemyUnitPrefab;
    [SerializeField] private AttackResolver attackResolver;
    
    private void Start()
    {
        SpawnEnemyUnits();
    }
    
    private void SpawnEnemyUnits()
    {
        List<EnemyUnit> enemyUnits = new List<EnemyUnit>();
        foreach (var enemySpawnPosition in enemySpawnPositions)
        {
            enemyUnits.Add(Instantiate(enemyUnitPrefab, enemySpawnPosition.position, Quaternion.identity, enemySpawnPosition));
        }
        attackResolver.SetNewEnemies(enemyUnits);
    }
}

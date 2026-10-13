using System;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Player : Unit
{
    public static Player Instance { get; private set; }

    // 게임오버 처리가 생기기 전까지는 이벤트만 발생시킵니다.
    public static event Action Died;

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        base.Awake();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    protected override void OnDied()
    {
        Debug.Log("Player died.");
        Died?.Invoke();
    }
}
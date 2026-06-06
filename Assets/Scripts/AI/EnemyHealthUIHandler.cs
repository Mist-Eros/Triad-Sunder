using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUIHandler : MonoBehaviour
{
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private RectTransform fillImage;

    void Update()
    {
        healthText.text = $"{healthComponent.CurrentHealth}/{healthComponent.MaxHealth}";
        fillImage.localScale = new Vector3(Math.Clamp(healthComponent.CurrentHealth / healthComponent.MaxHealth, 0, 1), 1, 1);
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;
        Vector3 dir = Camera.main.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 180f, 0f);
    }
}

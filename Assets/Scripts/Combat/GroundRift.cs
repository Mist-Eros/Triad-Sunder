using UnityEngine;
using System.Collections.Generic;

public class GroundRift : MonoBehaviour
{
    public float slowAmount = 0.7f;
    public float slowDuration = 5f;
    public float riftLifetime = 5f;
    public float damagePerTick = 5f;
    public float tickInterval = 0.5f;
    public Team ownerTeam;

    private float spawnTime;
    private float lastTickTime;
    private HashSet<HealthComponent> enemiesInRift = new HashSet<HealthComponent>();

    public void Initialize(float radius, float slowAmt, float slowDur, float lifetime, float dmgPerTick, Team team)
    {
        this.slowAmount = slowAmt;
        this.slowDuration = slowDur;
        this.riftLifetime = lifetime;
        this.damagePerTick = dmgPerTick;
        this.ownerTeam = team;

        transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
        spawnTime = Time.time;
        lastTickTime = Time.time;

        // Visual: dark semi-transparent plane
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
            mr = gameObject.AddComponent<MeshRenderer>();

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
            mf = gameObject.AddComponent<MeshFilter>();

        // Create a simple quad mesh
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader ?? Shader.Find("Standard"));
        mat.color = new Color(0.1f, 0.1f, 0.4f, 0.5f);
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mr.sharedMaterial = mat;
    }

    void Update()
    {
        float elapsed = Time.time - spawnTime;
        if (elapsed >= riftLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time - lastTickTime >= tickInterval)
        {
            lastTickTime = Time.time;
            TickDamage();
        }
    }

    void TickDamage()
    {
        Vector3 halfExtents = transform.localScale * 0.5f;
        Collider[] hits = Physics.OverlapBox(transform.position, halfExtents, Quaternion.identity);
        HashSet<HealthComponent> currentInRift = new HashSet<HealthComponent>();

        foreach (Collider hit in hits)
        {
            HealthComponent health = hit.GetComponentInParent<HealthComponent>();
            if (health == null || health.CurrentTeam == ownerTeam) continue;

            currentInRift.Add(health);

            // Apply slow to new enemies entering
            if (!enemiesInRift.Contains(health))
            {
                StatusEffect se = health.GetComponent<StatusEffect>();
                if (se == null) se = health.gameObject.AddComponent<StatusEffect>();
                se.ApplySlow(slowAmount, slowDuration);
            }

            // Deal damage
            health.TakeDamage(damagePerTick);
        }

        enemiesInRift = currentInRift;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.2f, 0.8f, 0.4f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}

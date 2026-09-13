#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Editor
{
    public static class PhysicsLayerSetupBuilder
    {
        public const string PlayerLayerName = "Player";
        public const string GroundLayerName = "Ground";
        public const string EnemyBodyLayerName = "EnemyBody";
        public const string EnemyHurtboxLayerName = "EnemyHurtbox";

        public static int PlayerLayer => LayerMask.NameToLayer(PlayerLayerName);
        public static int GroundLayer => LayerMask.NameToLayer(GroundLayerName);
        public static int EnemyBodyLayer => LayerMask.NameToLayer(EnemyBodyLayerName);

        public static LayerMask PlayerMask => LayerMask.GetMask(PlayerLayerName);
        public static LayerMask EnemyTargetMask => LayerMask.GetMask(EnemyBodyLayerName, EnemyHurtboxLayerName);

        public static void SetupPhysicsLayers()
        {
            ConfigureCollisionMatrix();
            Debug.Log("Everrealm physics layers configured. Player does not physically collide with EnemyBody.");
        }

        [InitializeOnLoadMethod]
        private static void ConfigureOnLoad()
        {
            ConfigureCollisionMatrix();
        }

        public static void ConfigureCollisionMatrix()
        {
            if (PlayerLayer < 0 || EnemyBodyLayer < 0)
                return;

            Physics2D.IgnoreLayerCollision(PlayerLayer, EnemyBodyLayer, true);
        }

        public static void AssignLayer(GameObject target, int layer)
        {
            if (target != null && layer >= 0)
                target.layer = layer;
        }
    }
}
#endif

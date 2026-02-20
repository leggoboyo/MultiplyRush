using UnityEngine;

namespace MultiplyRush
{
    public static class UnitVisualFactory
    {
        private static Material _friendlyUniform;
        private static Material _enemyUniform;
        private static Material _skin;
        private static Material _helmet;
        private static Material _weapon;
        private static Material _friendlyAccent;
        private static Material _enemyAccent;

        public static void ApplySoldierVisual(Transform unitRoot, bool enemy)
        {
            if (unitRoot == null || unitRoot.Find("SoldierModel") != null)
            {
                return;
            }

            var baseRenderer = unitRoot.GetComponent<MeshRenderer>();
            if (baseRenderer != null)
            {
                baseRenderer.enabled = false;
            }

            var modelRoot = new GameObject("SoldierModel").transform;
            modelRoot.SetParent(unitRoot, false);
            modelRoot.localPosition = Vector3.zero;
            modelRoot.localRotation = Quaternion.identity;
            modelRoot.localScale = Vector3.one;

            var uniform = enemy ? GetEnemyUniformMaterial() : GetFriendlyUniformMaterial();
            var accent = GetAccentMaterial(enemy);

            CreatePart(PrimitiveType.Capsule, "Torso", modelRoot, new Vector3(0f, 0.47f, 0f), new Vector3(0.22f, 0.3f, 0.15f), uniform);
            CreatePart(PrimitiveType.Cube, "ChestPlate", modelRoot, new Vector3(0f, 0.53f, 0.08f), new Vector3(0.2f, 0.2f, 0.07f), accent);
            CreatePart(PrimitiveType.Cube, "Backpack", modelRoot, new Vector3(0f, 0.53f, -0.11f), new Vector3(0.16f, 0.18f, 0.08f), accent);
            CreatePart(PrimitiveType.Sphere, "Head", modelRoot, new Vector3(0f, 0.86f, 0f), new Vector3(0.16f, 0.16f, 0.16f), GetSkinMaterial());
            CreatePart(PrimitiveType.Cube, "Helmet", modelRoot, new Vector3(0f, 0.93f, -0.01f), new Vector3(0.22f, 0.09f, 0.22f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "Visor", modelRoot, new Vector3(0f, 0.9f, 0.1f), new Vector3(0.12f, 0.04f, 0.02f), accent);
            CreatePart(PrimitiveType.Cube, "RifleBody", modelRoot, new Vector3(0f, 0.53f, 0.16f), new Vector3(0.26f, 0.05f, 0.05f), GetWeaponMaterial());
            CreatePart(PrimitiveType.Cube, "RifleBarrel", modelRoot, new Vector3(0f, 0.54f, 0.27f), new Vector3(0.03f, 0.03f, 0.16f), GetWeaponMaterial());
            CreatePart(PrimitiveType.Cube, "LeftArm", modelRoot, new Vector3(-0.14f, 0.53f, 0.02f), new Vector3(0.05f, 0.16f, 0.05f), uniform);
            CreatePart(PrimitiveType.Cube, "RightArm", modelRoot, new Vector3(0.14f, 0.53f, 0.02f), new Vector3(0.05f, 0.16f, 0.05f), uniform);
            CreatePart(PrimitiveType.Cube, "Legs", modelRoot, new Vector3(0f, 0.18f, 0f), new Vector3(0.2f, 0.26f, 0.13f), uniform);
            CreatePart(PrimitiveType.Cube, "KneePads", modelRoot, new Vector3(0f, 0.09f, 0.06f), new Vector3(0.2f, 0.05f, 0.05f), accent);
            CreatePart(PrimitiveType.Cube, "Boots", modelRoot, new Vector3(0f, 0.04f, 0.03f), new Vector3(0.2f, 0.06f, 0.12f), GetHelmetMaterial());
        }

        private static void CreatePart(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Vector3 localEuler = default)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEuler);
            part.transform.localScale = localScale;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = part.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static Material GetFriendlyUniformMaterial()
        {
            if (_friendlyUniform != null)
            {
                return _friendlyUniform;
            }

            _friendlyUniform = CreateRuntimeMaterial("FriendlyUniform", new Color(0.19f, 0.66f, 0.98f, 1f), 0.32f);
            return _friendlyUniform;
        }

        private static Material GetEnemyUniformMaterial()
        {
            if (_enemyUniform != null)
            {
                return _enemyUniform;
            }

            _enemyUniform = CreateRuntimeMaterial("EnemyUniform", new Color(0.96f, 0.36f, 0.32f, 1f), 0.32f);
            return _enemyUniform;
        }

        private static Material GetSkinMaterial()
        {
            if (_skin != null)
            {
                return _skin;
            }

            _skin = CreateRuntimeMaterial("SoldierSkin", new Color(0.98f, 0.78f, 0.62f, 1f), 0.2f);
            return _skin;
        }

        private static Material GetHelmetMaterial()
        {
            if (_helmet != null)
            {
                return _helmet;
            }

            _helmet = CreateRuntimeMaterial("SoldierHelmet", new Color(0.08f, 0.12f, 0.2f, 1f), 0.24f);
            return _helmet;
        }

        private static Material GetWeaponMaterial()
        {
            if (_weapon != null)
            {
                return _weapon;
            }

            _weapon = CreateRuntimeMaterial("SoldierWeapon", new Color(0.22f, 0.24f, 0.28f, 1f), 0.1f);
            return _weapon;
        }

        private static Material GetAccentMaterial(bool enemy)
        {
            if (enemy)
            {
                if (_enemyAccent != null)
                {
                    return _enemyAccent;
                }

                _enemyAccent = CreateRuntimeMaterial(
                    "EnemyAccent",
                    new Color(1f, 0.68f, 0.42f, 1f),
                    0.38f);
                return _enemyAccent;
            }

            if (_friendlyAccent != null)
            {
                return _friendlyAccent;
            }

            _friendlyAccent = CreateRuntimeMaterial(
                "FriendlyAccent",
                new Color(0.56f, 0.9f, 1f, 1f),
                0.38f);
            return _friendlyAccent;
        }

        private static Material CreateRuntimeMaterial(string name, Color color, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = name
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            }

            return material;
        }
    }
}

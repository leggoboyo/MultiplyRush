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

            CreatePart(PrimitiveType.Capsule, "Torso", modelRoot, new Vector3(0f, 0.54f, 0f), new Vector3(0.24f, 0.4f, 0.18f), uniform);
            CreatePart(PrimitiveType.Cube, "ChestPlate", modelRoot, new Vector3(0f, 0.59f, 0.1f), new Vector3(0.26f, 0.24f, 0.1f), accent);
            CreatePart(PrimitiveType.Cube, "ChestRig", modelRoot, new Vector3(0f, 0.51f, 0.12f), new Vector3(0.23f, 0.13f, 0.08f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "NeckGuard", modelRoot, new Vector3(0f, 0.77f, 0.03f), new Vector3(0.15f, 0.06f, 0.12f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "ShoulderPadL", modelRoot, new Vector3(-0.17f, 0.67f, 0.05f), new Vector3(0.11f, 0.11f, 0.11f), accent);
            CreatePart(PrimitiveType.Cube, "ShoulderPadR", modelRoot, new Vector3(0.17f, 0.67f, 0.05f), new Vector3(0.11f, 0.11f, 0.11f), accent);
            CreatePart(PrimitiveType.Cube, "Backpack", modelRoot, new Vector3(0f, 0.57f, -0.15f), new Vector3(0.18f, 0.24f, 0.12f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Sphere, "Head", modelRoot, new Vector3(0f, 0.95f, 0f), new Vector3(0.18f, 0.18f, 0.18f), GetSkinMaterial());
            CreatePart(PrimitiveType.Cube, "Helmet", modelRoot, new Vector3(0f, 1.03f, -0.01f), new Vector3(0.28f, 0.12f, 0.27f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "HelmetBrim", modelRoot, new Vector3(0f, 0.96f, 0.14f), new Vector3(0.18f, 0.04f, 0.05f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "Visor", modelRoot, new Vector3(0f, 0.96f, 0.12f), new Vector3(0.16f, 0.05f, 0.025f), accent);
            CreatePart(PrimitiveType.Cube, "RifleBody", modelRoot, new Vector3(0f, 0.57f, 0.26f), new Vector3(0.34f, 0.07f, 0.07f), GetWeaponMaterial(), new Vector3(-6f, 0f, 0f));
            var rifleBarrel = CreatePart(PrimitiveType.Cube, "RifleBarrel", modelRoot, new Vector3(0f, 0.56f, 0.47f), new Vector3(0.04f, 0.04f, 0.3f), GetWeaponMaterial(), new Vector3(-6f, 0f, 0f));
            CreatePart(PrimitiveType.Cube, "RifleStock", modelRoot, new Vector3(0f, 0.59f, 0.12f), new Vector3(0.17f, 0.08f, 0.09f), GetWeaponMaterial(), new Vector3(-6f, 0f, 0f));
            CreatePart(PrimitiveType.Cube, "RifleGrip", modelRoot, new Vector3(0f, 0.5f, 0.22f), new Vector3(0.07f, 0.12f, 0.05f), GetWeaponMaterial(), new Vector3(-16f, 0f, 0f));
            var muzzlePoint = new GameObject("MuzzlePoint").transform;
            muzzlePoint.SetParent(rifleBarrel != null ? rifleBarrel : modelRoot, false);
            muzzlePoint.localPosition = rifleBarrel != null ? new Vector3(0f, 0f, 0.62f) : new Vector3(0f, 0.56f, 0.58f);
            muzzlePoint.localRotation = Quaternion.identity;
            muzzlePoint.localScale = Vector3.one;
            CreatePart(PrimitiveType.Cube, "LeftArm", modelRoot, new Vector3(-0.16f, 0.55f, 0.06f), new Vector3(0.07f, 0.2f, 0.07f), uniform, new Vector3(-24f, 0f, 10f));
            CreatePart(PrimitiveType.Cube, "RightArm", modelRoot, new Vector3(0.16f, 0.55f, 0.06f), new Vector3(0.07f, 0.2f, 0.07f), uniform, new Vector3(-38f, 0f, -10f));
            CreatePart(PrimitiveType.Cube, "LeftForearm", modelRoot, new Vector3(-0.15f, 0.46f, 0.16f), new Vector3(0.06f, 0.14f, 0.06f), uniform, new Vector3(-58f, 0f, 8f));
            CreatePart(PrimitiveType.Cube, "RightForearm", modelRoot, new Vector3(0.15f, 0.47f, 0.16f), new Vector3(0.06f, 0.14f, 0.06f), uniform, new Vector3(-64f, 0f, -8f));
            CreatePart(PrimitiveType.Cube, "Hip", modelRoot, new Vector3(0f, 0.31f, 0f), new Vector3(0.24f, 0.11f, 0.14f), uniform);
            CreatePart(PrimitiveType.Cube, "LeftLeg", modelRoot, new Vector3(-0.08f, 0.19f, 0f), new Vector3(0.1f, 0.24f, 0.12f), uniform);
            CreatePart(PrimitiveType.Cube, "RightLeg", modelRoot, new Vector3(0.08f, 0.19f, 0f), new Vector3(0.1f, 0.24f, 0.12f), uniform);
            CreatePart(PrimitiveType.Cube, "LeftShin", modelRoot, new Vector3(-0.08f, 0.07f, 0.01f), new Vector3(0.09f, 0.16f, 0.11f), uniform);
            CreatePart(PrimitiveType.Cube, "RightShin", modelRoot, new Vector3(0.08f, 0.07f, 0.01f), new Vector3(0.09f, 0.16f, 0.11f), uniform);
            CreatePart(PrimitiveType.Cube, "LeftKneePad", modelRoot, new Vector3(-0.08f, 0.21f, 0.07f), new Vector3(0.1f, 0.07f, 0.05f), accent);
            CreatePart(PrimitiveType.Cube, "RightKneePad", modelRoot, new Vector3(0.08f, 0.21f, 0.07f), new Vector3(0.1f, 0.07f, 0.05f), accent);
            CreatePart(PrimitiveType.Cube, "LeftBoot", modelRoot, new Vector3(-0.08f, -0.03f, 0.05f), new Vector3(0.12f, 0.08f, 0.18f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "RightBoot", modelRoot, new Vector3(0.08f, -0.03f, 0.05f), new Vector3(0.12f, 0.08f, 0.18f), GetHelmetMaterial());
        }

        private static Transform CreatePart(
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

            return part.transform;
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

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
        private static Material _gear;
        private static Material _strap;
        private static Material _friendlyVisor;
        private static Material _enemyVisor;

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
            var visor = GetVisorMaterial(enemy);
            var gear = GetGearMaterial();
            var strap = GetStrapMaterial();

            CreatePart(PrimitiveType.Capsule, "Torso", modelRoot, new Vector3(0f, 0.56f, 0f), new Vector3(0.24f, 0.42f, 0.18f), uniform);
            CreatePart(PrimitiveType.Cube, "ChestPlate", modelRoot, new Vector3(0f, 0.62f, 0.1f), new Vector3(0.28f, 0.24f, 0.12f), gear);
            CreatePart(PrimitiveType.Cube, "VestUpper", modelRoot, new Vector3(0f, 0.68f, 0.08f), new Vector3(0.24f, 0.11f, 0.08f), accent);
            CreatePart(PrimitiveType.Cube, "VestLower", modelRoot, new Vector3(0f, 0.5f, 0.1f), new Vector3(0.23f, 0.12f, 0.08f), gear);
            CreatePart(PrimitiveType.Cube, "ChestStrapL", modelRoot, new Vector3(-0.09f, 0.58f, 0.13f), new Vector3(0.03f, 0.24f, 0.03f), strap, new Vector3(0f, 0f, 10f));
            CreatePart(PrimitiveType.Cube, "ChestStrapR", modelRoot, new Vector3(0.09f, 0.58f, 0.13f), new Vector3(0.03f, 0.24f, 0.03f), strap, new Vector3(0f, 0f, -10f));
            CreatePart(PrimitiveType.Cube, "NeckGuard", modelRoot, new Vector3(0f, 0.79f, 0.03f), new Vector3(0.16f, 0.06f, 0.13f), gear);
            CreatePart(PrimitiveType.Cube, "ShoulderPadL", modelRoot, new Vector3(-0.17f, 0.67f, 0.06f), new Vector3(0.11f, 0.1f, 0.1f), accent);
            CreatePart(PrimitiveType.Cube, "ShoulderPadR", modelRoot, new Vector3(0.17f, 0.67f, 0.06f), new Vector3(0.11f, 0.1f, 0.1f), accent);
            CreatePart(PrimitiveType.Cube, "Backpack", modelRoot, new Vector3(0f, 0.58f, -0.15f), new Vector3(0.19f, 0.25f, 0.12f), gear);
            CreatePart(PrimitiveType.Cube, "BackpackRoll", modelRoot, new Vector3(0f, 0.73f, -0.14f), new Vector3(0.17f, 0.08f, 0.1f), strap);

            CreatePart(PrimitiveType.Sphere, "Head", modelRoot, new Vector3(0f, 0.95f, 0f), new Vector3(0.17f, 0.17f, 0.17f), GetSkinMaterial());
            CreatePart(PrimitiveType.Cube, "FaceMask", modelRoot, new Vector3(0f, 0.9f, 0.11f), new Vector3(0.16f, 0.07f, 0.05f), strap);
            CreatePart(PrimitiveType.Cube, "Helmet", modelRoot, new Vector3(0f, 1.02f, -0.01f), new Vector3(0.27f, 0.12f, 0.26f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "HelmetBrim", modelRoot, new Vector3(0f, 0.97f, 0.14f), new Vector3(0.19f, 0.04f, 0.05f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "EarGuardL", modelRoot, new Vector3(-0.12f, 0.96f, -0.01f), new Vector3(0.03f, 0.09f, 0.12f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "EarGuardR", modelRoot, new Vector3(0.12f, 0.96f, -0.01f), new Vector3(0.03f, 0.09f, 0.12f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "Visor", modelRoot, new Vector3(0f, 0.95f, 0.13f), new Vector3(0.16f, 0.05f, 0.024f), visor);

            CreatePart(PrimitiveType.Cube, "RifleBody", modelRoot, new Vector3(0f, 0.57f, 0.26f), new Vector3(0.35f, 0.07f, 0.08f), GetWeaponMaterial(), new Vector3(-7f, 0f, 0f));
            var rifleBarrel = CreatePart(PrimitiveType.Cube, "RifleBarrel", modelRoot, new Vector3(0f, 0.56f, 0.48f), new Vector3(0.05f, 0.05f, 0.34f), GetWeaponMaterial(), new Vector3(-7f, 0f, 0f));
            CreatePart(PrimitiveType.Cube, "RifleStock", modelRoot, new Vector3(0f, 0.6f, 0.12f), new Vector3(0.18f, 0.09f, 0.09f), GetWeaponMaterial(), new Vector3(-7f, 0f, 0f));
            CreatePart(PrimitiveType.Cube, "RifleGrip", modelRoot, new Vector3(0f, 0.5f, 0.23f), new Vector3(0.07f, 0.13f, 0.05f), GetWeaponMaterial(), new Vector3(-18f, 0f, 0f));
            CreatePart(PrimitiveType.Cube, "RifleMagazine", modelRoot, new Vector3(0f, 0.49f, 0.3f), new Vector3(0.07f, 0.12f, 0.06f), GetWeaponMaterial(), new Vector3(-10f, 0f, 0f));
            CreatePart(PrimitiveType.Cube, "RifleRail", modelRoot, new Vector3(0f, 0.61f, 0.29f), new Vector3(0.22f, 0.025f, 0.18f), strap, new Vector3(-7f, 0f, 0f));

            var muzzlePoint = new GameObject("MuzzlePoint").transform;
            muzzlePoint.SetParent(rifleBarrel != null ? rifleBarrel : modelRoot, false);
            muzzlePoint.localPosition = rifleBarrel != null ? new Vector3(0f, 0f, 0.55f) : new Vector3(0f, 0.56f, 0.64f);
            muzzlePoint.localRotation = Quaternion.identity;
            muzzlePoint.localScale = Vector3.one;

            CreatePart(PrimitiveType.Cube, "LeftArm", modelRoot, new Vector3(-0.16f, 0.56f, 0.05f), new Vector3(0.07f, 0.2f, 0.08f), uniform, new Vector3(-22f, 0f, 12f));
            CreatePart(PrimitiveType.Cube, "RightArm", modelRoot, new Vector3(0.16f, 0.56f, 0.05f), new Vector3(0.07f, 0.2f, 0.08f), uniform, new Vector3(-34f, 0f, -12f));
            CreatePart(PrimitiveType.Cube, "LeftForearm", modelRoot, new Vector3(-0.15f, 0.46f, 0.17f), new Vector3(0.06f, 0.15f, 0.07f), uniform, new Vector3(-60f, 0f, 8f));
            CreatePart(PrimitiveType.Cube, "RightForearm", modelRoot, new Vector3(0.15f, 0.47f, 0.17f), new Vector3(0.06f, 0.15f, 0.07f), uniform, new Vector3(-68f, 0f, -8f));

            CreatePart(PrimitiveType.Cube, "Hip", modelRoot, new Vector3(0f, 0.31f, 0f), new Vector3(0.24f, 0.11f, 0.15f), uniform);
            CreatePart(PrimitiveType.Cube, "UtilityBelt", modelRoot, new Vector3(0f, 0.35f, 0.03f), new Vector3(0.26f, 0.05f, 0.14f), strap);
            CreatePart(PrimitiveType.Cube, "PouchL", modelRoot, new Vector3(-0.13f, 0.32f, 0.08f), new Vector3(0.06f, 0.08f, 0.05f), gear);
            CreatePart(PrimitiveType.Cube, "PouchR", modelRoot, new Vector3(0.13f, 0.32f, 0.08f), new Vector3(0.06f, 0.08f, 0.05f), gear);

            CreatePart(PrimitiveType.Cube, "LeftLeg", modelRoot, new Vector3(-0.08f, 0.2f, 0f), new Vector3(0.11f, 0.24f, 0.13f), uniform);
            CreatePart(PrimitiveType.Cube, "RightLeg", modelRoot, new Vector3(0.08f, 0.2f, 0f), new Vector3(0.11f, 0.24f, 0.13f), uniform);
            CreatePart(PrimitiveType.Cube, "LeftShin", modelRoot, new Vector3(-0.08f, 0.07f, 0.01f), new Vector3(0.095f, 0.16f, 0.12f), uniform);
            CreatePart(PrimitiveType.Cube, "RightShin", modelRoot, new Vector3(0.08f, 0.07f, 0.01f), new Vector3(0.095f, 0.16f, 0.12f), uniform);
            CreatePart(PrimitiveType.Cube, "LeftKneePad", modelRoot, new Vector3(-0.08f, 0.21f, 0.075f), new Vector3(0.11f, 0.07f, 0.05f), accent);
            CreatePart(PrimitiveType.Cube, "RightKneePad", modelRoot, new Vector3(0.08f, 0.21f, 0.075f), new Vector3(0.11f, 0.07f, 0.05f), accent);
            CreatePart(PrimitiveType.Cube, "LeftBoot", modelRoot, new Vector3(-0.08f, -0.03f, 0.05f), new Vector3(0.13f, 0.08f, 0.19f), GetHelmetMaterial());
            CreatePart(PrimitiveType.Cube, "RightBoot", modelRoot, new Vector3(0.08f, -0.03f, 0.05f), new Vector3(0.13f, 0.08f, 0.19f), GetHelmetMaterial());
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

            _friendlyUniform = CreateRuntimeMaterial("FriendlyUniform", new Color(0.2f, 0.63f, 0.9f, 1f), 0.24f);
            return _friendlyUniform;
        }

        private static Material GetEnemyUniformMaterial()
        {
            if (_enemyUniform != null)
            {
                return _enemyUniform;
            }

            _enemyUniform = CreateRuntimeMaterial("EnemyUniform", new Color(0.82f, 0.34f, 0.3f, 1f), 0.24f);
            return _enemyUniform;
        }

        private static Material GetSkinMaterial()
        {
            if (_skin != null)
            {
                return _skin;
            }

            _skin = CreateRuntimeMaterial("SoldierSkin", new Color(0.97f, 0.77f, 0.61f, 1f), 0.2f);
            return _skin;
        }

        private static Material GetHelmetMaterial()
        {
            if (_helmet != null)
            {
                return _helmet;
            }

            _helmet = CreateRuntimeMaterial("SoldierHelmet", new Color(0.08f, 0.12f, 0.19f, 1f), 0.18f);
            return _helmet;
        }

        private static Material GetWeaponMaterial()
        {
            if (_weapon != null)
            {
                return _weapon;
            }

            _weapon = CreateRuntimeMaterial("SoldierWeapon", new Color(0.2f, 0.24f, 0.3f, 1f), 0.12f);
            return _weapon;
        }

        private static Material GetGearMaterial()
        {
            if (_gear != null)
            {
                return _gear;
            }

            _gear = CreateRuntimeMaterial("SoldierGear", new Color(0.13f, 0.17f, 0.22f, 1f), 0.13f);
            return _gear;
        }

        private static Material GetStrapMaterial()
        {
            if (_strap != null)
            {
                return _strap;
            }

            _strap = CreateRuntimeMaterial("SoldierStrap", new Color(0.11f, 0.13f, 0.16f, 1f), 0.09f);
            return _strap;
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
                    new Color(0.97f, 0.6f, 0.42f, 1f),
                    0.32f);
                return _enemyAccent;
            }

            if (_friendlyAccent != null)
            {
                return _friendlyAccent;
            }

            _friendlyAccent = CreateRuntimeMaterial(
                "FriendlyAccent",
                new Color(0.57f, 0.9f, 1f, 1f),
                0.32f);
            return _friendlyAccent;
        }

        private static Material GetVisorMaterial(bool enemy)
        {
            if (enemy)
            {
                if (_enemyVisor != null)
                {
                    return _enemyVisor;
                }

                _enemyVisor = CreateRuntimeMaterial("EnemyVisor", new Color(1f, 0.58f, 0.5f, 1f), 0.5f);
                return _enemyVisor;
            }

            if (_friendlyVisor != null)
            {
                return _friendlyVisor;
            }

            _friendlyVisor = CreateRuntimeMaterial("FriendlyVisor", new Color(0.62f, 0.95f, 1f, 1f), 0.5f);
            return _friendlyVisor;
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

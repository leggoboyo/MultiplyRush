using System;
using System.Collections;
using UnityEngine;

namespace MultiplyRush
{
    internal static class UnitDeathFx
    {
        public static void Spawn(
            MonoBehaviour host,
            Transform sourceUnit,
            float duration,
            Vector3 directionalImpulse,
            float randomImpulse,
            float gravity,
            float shrinkStart,
            float endScale,
            Action<Vector3> onFinished = null,
            float upwardImpulseMin = 1f,
            float upwardImpulseMax = 2.8f,
            float startDelay = 0f)
        {
            if (host == null || sourceUnit == null || sourceUnit.gameObject == null)
            {
                return;
            }

            var clone = UnityEngine.Object.Instantiate(sourceUnit.gameObject, sourceUnit.position, sourceUnit.rotation);
            if (clone == null)
            {
                return;
            }

            clone.name = sourceUnit.name + "_DeathFx";
            clone.SetActive(true);
            StripInteractiveComponents(clone);
            host.StartCoroutine(AnimateDeathClone(
                clone.transform,
                duration,
                directionalImpulse,
                randomImpulse,
                gravity,
                shrinkStart,
                endScale,
                onFinished,
                upwardImpulseMin,
                upwardImpulseMax,
                startDelay));
        }

        private static void StripInteractiveComponents(GameObject clone)
        {
            if (clone == null)
            {
                return;
            }

            var colliders = clone.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider != null)
                {
                    UnityEngine.Object.Destroy(collider);
                }
            }

            var rigidbodies = clone.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < rigidbodies.Length; i++)
            {
                var rigidbody = rigidbodies[i];
                if (rigidbody != null)
                {
                    UnityEngine.Object.Destroy(rigidbody);
                }
            }

            var scripts = clone.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < scripts.Length; i++)
            {
                var script = scripts[i];
                if (script != null)
                {
                    UnityEngine.Object.Destroy(script);
                }
            }

            var particles = clone.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (particle != null)
                {
                    UnityEngine.Object.Destroy(particle.gameObject);
                }
            }
        }

        private static IEnumerator AnimateDeathClone(
            Transform clone,
            float duration,
            Vector3 directionalImpulse,
            float randomImpulse,
            float gravity,
            float shrinkStart,
            float endScale,
            Action<Vector3> onFinished,
            float upwardImpulseMin,
            float upwardImpulseMax,
            float startDelay)
        {
            if (clone == null)
            {
                yield break;
            }

            var safeDuration = Mathf.Max(0.14f, duration);
            var shrinkThreshold = Mathf.Clamp01(shrinkStart);
            var targetScale = Vector3.one * Mathf.Clamp(endScale, 0f, 0.85f);
            var baseScale = clone.localScale;
            var elapsed = 0f;
            if (startDelay > 0f)
            {
                var remainingDelay = Mathf.Max(0f, startDelay);
                while (remainingDelay > 0f)
                {
                    if (clone == null)
                    {
                        yield break;
                    }

                    remainingDelay -= Mathf.Max(0.001f, Time.deltaTime);
                    yield return null;
                }
            }

            var safeUpMin = Mathf.Min(upwardImpulseMin, upwardImpulseMax);
            var safeUpMax = Mathf.Max(upwardImpulseMin, upwardImpulseMax);
            var velocity = directionalImpulse + new Vector3(
                UnityEngine.Random.Range(-randomImpulse, randomImpulse),
                UnityEngine.Random.Range(safeUpMin, safeUpMax),
                UnityEngine.Random.Range(-randomImpulse, randomImpulse));
            var angularVelocity = new Vector3(
                UnityEngine.Random.Range(-220f, 220f),
                UnityEngine.Random.Range(-180f, 180f),
                UnityEngine.Random.Range(-260f, 260f));

            while (elapsed < safeDuration)
            {
                if (clone == null)
                {
                    yield break;
                }

                var deltaTime = Mathf.Max(0.001f, Time.deltaTime);
                elapsed += deltaTime;
                velocity += Vector3.up * (-Mathf.Max(6f, gravity) * deltaTime);
                clone.position += velocity * deltaTime;
                clone.Rotate(angularVelocity * deltaTime, Space.Self);

                var normalized = Mathf.Clamp01(elapsed / safeDuration);
                if (normalized >= shrinkThreshold)
                {
                    var shrinkT = Mathf.InverseLerp(shrinkThreshold, 1f, normalized);
                    clone.localScale = Vector3.Lerp(baseScale, Vector3.Scale(baseScale, targetScale), shrinkT);
                }

                yield return null;
            }

            if (clone != null)
            {
                onFinished?.Invoke(clone.position);
                UnityEngine.Object.Destroy(clone.gameObject);
            }
        }
    }
}

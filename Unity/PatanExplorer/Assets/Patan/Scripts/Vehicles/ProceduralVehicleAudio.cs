using UnityEngine;

namespace PatanExplorer.Vehicles
{
    public sealed class ProceduralVehicleAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip engineLoop;
        [SerializeField] private AudioClip roadLoop;
        [SerializeField] private AudioClip skidLoop;
        [SerializeField] private AudioClip startClip;
        [SerializeField] private AudioClip impactClip;

        private ArcadeVehicleController vehicleController;
        private AudioSource engineSource;
        private AudioSource roadSource;
        private AudioSource skidSource;
        private AudioSource oneShotSource;
        private bool isEngineRunning;

        public void Configure(AudioClip engine, AudioClip road, AudioClip skid, AudioClip start, AudioClip impact)
        {
            engineLoop = engine;
            roadLoop = road;
            skidLoop = skid;
            startClip = start;
            impactClip = impact;
        }

        public void SetEngineRunning(bool isRunning)
        {
            if (isEngineRunning == isRunning || engineSource == null)
            {
                return;
            }

            isEngineRunning = isRunning;
            if (isRunning)
            {
                if (!engineSource.isPlaying)
                {
                    engineSource.Play();
                    roadSource.Play();
                    skidSource.Play();
                }

                oneShotSource.PlayOneShot(startClip, 0.8f);
            }
            else
            {
                engineSource.Stop();
                roadSource.Stop();
                skidSource.Stop();
            }
        }

        private void Awake()
        {
            vehicleController = GetComponent<ArcadeVehicleController>();
            engineSource = CreateSource(true, 0.35f);
            roadSource = CreateSource(true, 0.55f);
            skidSource = CreateSource(true, 0.55f);
            oneShotSource = CreateSource(false, 0.45f);
            engineSource.clip = engineLoop;
            roadSource.clip = roadLoop;
            skidSource.clip = skidLoop;
        }

        private void Update()
        {
            if (!isEngineRunning || vehicleController == null)
            {
                return;
            }

            float speed = vehicleController.NormalizedSpeed;
            float throttle = vehicleController.ThrottleAmount;
            engineSource.pitch = 0.72f + speed * 1.25f + throttle * 0.22f;
            engineSource.volume = 0.3f + speed * 0.3f + throttle * 0.16f;
            roadSource.pitch = 0.7f + speed * 0.85f;
            roadSource.volume = Mathf.Lerp(0f, 0.32f, speed);
            float skidAmount = Mathf.Clamp01(vehicleController.LateralSlip * 1.4f + (vehicleController.IsBraking ? speed : 0f));
            skidSource.pitch = 0.85f + speed * 0.35f;
            skidSource.volume = skidAmount * 0.34f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < 3f || impactClip == null || oneShotSource == null)
            {
                return;
            }

            oneShotSource.PlayOneShot(impactClip, Mathf.Clamp01((impactSpeed - 2f) / 12f) * 0.65f);
        }

        private AudioSource CreateSource(bool shouldLoop, float spatialBlend)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = shouldLoop;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0.25f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = 45f;
            source.volume = shouldLoop ? 0f : 1f;
            return source;
        }
    }
}

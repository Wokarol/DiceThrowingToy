using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Wokarol.Common;
using Wokarol.GameSystemsLocator;
using Wokarol.Utils;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using Random = UnityEngine.Random;

namespace Wokarol
{

    public class DiceSpawner : MonoBehaviour
    {
        [SerializeField] private Dice dice;
        [Space]
        [SerializeField] private float minForce = 10;
        [SerializeField] private float maxForce = 50;
        [SerializeField] private float minTorque = 10;
        [SerializeField] private float maxTorque = 50;
        [Space]
        [SerializeField] private float throwDirectionMaxAngle = 10;
        [Space]
        [SerializeField] private SimulationEndThreshold simulationEndThreshold = SimulationEndThreshold.DiceSleeping;
        [SerializeField] private bool keepObjectsDynamicAfterThrow = true;
        [SerializeField] private List<GameObject> rootObjectsToSearchForDynamicsIn = new();
        [Space]
        [Space]
        [SerializeField] private bool extraDebugData = false;

        List<Rigidbody> sceneDynamicObjects = new();

        List<Rigidbody> allDynamicObjects = new();
        List<Dice> diceRolledThisThrow = new();

        List<SimulatedPhysicsFramesData> computedAnimation = new();
        private bool isRollingDice;

        private void Start()
        {
            foreach (var r in rootObjectsToSearchForDynamicsIn)
            {
                foreach (var candidate in r.GetComponentsInChildren<Rigidbody>())
                {
                    if (candidate.isKinematic) continue;

                    sceneDynamicObjects.Add(candidate);
                    allDynamicObjects.Add(candidate);
                    computedAnimation.Add(new());
                }
            }
        }

        private void Update()
        {
            if (GameSystems.Get<InputBlocker>().IsBlocked)
                return;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                var isShiftDown = Keyboard.current.shiftKey.isPressed;

                if (isShiftDown)
                {
                    RollDice(1);
                }
                else
                {
                    RollDice(1, 1, 1, 1, 1);
                }
            }
        }

        public void RollDice(params int[] values)
        {
            RollDiceAsync(values).Forget();
        }

        private async UniTask RollDiceAsync(params int[] values)
        {
            var ct = gameObject.GetCancellationTokenOnDestroy();

            if (isRollingDice)
            {
                Debug.LogWarning($"{LogExtras.Prefix(this)} Could not roll the dice, an existing roll is in progesss");
                return; // Interlock
            }

            isRollingDice = true;

            allDynamicObjects.RemoveAll(d => d == null);

            diceRolledThisThrow.Clear();

            for (int i = 0; i < values.Length; i++)
            {
                var offset = Quaternion.AngleAxis((i / (float)values.Length) * 360f, transform.forward) * (transform.up * 1.5f);

                var d = Instantiate(dice, transform.position + offset, transform.rotation);

                var angle = Random.Range(-throwDirectionMaxAngle, throwDirectionMaxAngle);
                var force = Random.Range(minForce, maxForce);
                var torqueForce = Random.Range(minTorque, maxTorque);
                var torqueAxis = Random.onUnitSphere;

                d.Body.AddForce(force * (Quaternion.AngleAxis(angle, Vector3.up) * transform.forward), ForceMode.Impulse);
                d.Body.AddTorque(torqueForce * torqueAxis, ForceMode.Impulse);

                allDynamicObjects.Add(d.Body);
                diceRolledThisThrow.Add(d);
                computedAnimation.Add(new());
            }

            if (Keyboard.current.ctrlKey.isPressed)
                Debug.Break();

            Physics.simulationMode = SimulationMode.Script;

            Physics.Simulate(Time.fixedDeltaTime);

            int maxSteps = (int)(20f / Time.fixedDeltaTime);
            for (int step = 0; step <= maxSteps; step++)
            {
                Physics.Simulate(Time.fixedDeltaTime);

                bool isSimulationReadyToStop = true;

                for (int i = 0; i < allDynamicObjects.Count; i++)
                {
                    var d = allDynamicObjects[i];

                    if (simulationEndThreshold is SimulationEndThreshold.AllSleeping or SimulationEndThreshold.LowVelocity)
                    {
                        if (simulationEndThreshold is SimulationEndThreshold.AllSleeping && !d.IsSleeping())
                            isSimulationReadyToStop = false; 

                        if (simulationEndThreshold is SimulationEndThreshold.LowVelocity && (d.linearVelocity.magnitude > 0.2f || d.angularVelocity.magnitude > 0.2f))
                            isSimulationReadyToStop = false;
                    }

                    computedAnimation[i].Frames.Enqueue(new()
                    {
                        Position = d.position,
                        Rotation = d.rotation,
                    });

                    computedAnimation[i].LastLinearVelocity = d.linearVelocity;
                    computedAnimation[i].LastAngularVelocity = d.angularVelocity;
                    computedAnimation[i].WasSleepingAtTheEnd = d.IsSleeping();
                }

                if (simulationEndThreshold is SimulationEndThreshold.DiceSleeping)
                {
                    for (int i = 0; i < diceRolledThisThrow.Count; i++)
                    {
                        if (!diceRolledThisThrow[i].Body.IsSleeping())
                            isSimulationReadyToStop = false;
                    }

                }

                if (isSimulationReadyToStop)
                {
                    Debug.Log($"{LogExtras.Prefix(this)} Simulated the roll in {LogExtras.Value(step)} steps");
                    break;
                }
                else if (step == maxSteps)
                {
                    Debug.LogWarning($"{LogExtras.Prefix(this)} Reached the simulation step limit");
                }
            }

            var steps = computedAnimation[0].Frames.Count;

            // We prep the dice for the animation
            for (int i = 0; i < allDynamicObjects.Count; i++)
            {
                var d = allDynamicObjects[i];
                d.isKinematic = true;
            }

            for (int i = 0; i < diceRolledThisThrow.Count; i++)
            {
                var d = diceRolledThisThrow[i];
                d.ForceValue(values[i]);
            }

            // We reset the dice
            for (int i = 0; i < allDynamicObjects.Count; i++)
            {
                var frame = computedAnimation[i].Frames.Dequeue();

                allDynamicObjects[i].transform.position = frame.Position;
                allDynamicObjects[i].transform.rotation = frame.Rotation;
            }

            // The simulation starts again
            Physics.simulationMode = SimulationMode.FixedUpdate;

            for (int step = 0; step < (steps - 1); step++) // The first step is handled by reseting
            {
                await UniTask.WaitForFixedUpdate(ct);

                for (int i = 0; i < allDynamicObjects.Count; i++)
                {
                    var frame = computedAnimation[i].Frames.Dequeue();

                    allDynamicObjects[i].position = frame.Position;
                    allDynamicObjects[i].rotation = frame.Rotation;
                }
            }

            // We clean up and revert the things to correct state
            if (keepObjectsDynamicAfterThrow)
            {
                for (int i = 0; i < allDynamicObjects.Count; i++)
                {
                    var d = allDynamicObjects[i];
                    d.isKinematic = false;
                    allDynamicObjects[i].linearVelocity = computedAnimation[i].LastLinearVelocity;
                    allDynamicObjects[i].angularVelocity = computedAnimation[i].LastAngularVelocity;

                    if (computedAnimation[i].WasSleepingAtTheEnd)
                        allDynamicObjects[i].Sleep();
                }
            }
            else
            {
                allDynamicObjects.Clear();
                computedAnimation.Clear();
            }

            isRollingDice = false;
        }

        private void OnGUI()
        {
            if (!extraDebugData) return;

            int dices = 0;
            int framesMin = 0;
            int framesMax = 0;


            if (allDynamicObjects.Count > 0)
            {
                Span<int> framesPerDice = stackalloc int[allDynamicObjects.Count];
                dices = allDynamicObjects.Count;

                for (int i = 0; i < allDynamicObjects.Count; i++)
                {
                    framesPerDice[i] = computedAnimation[i]?.Frames?.Count ?? 0;
                }

                framesMin = 9999999;
                framesMax = 0;

                for (int i = 0; i < framesPerDice.Length; i++)
                {
                    if (framesPerDice[i] < framesMin)
                        framesMin = framesPerDice[i];

                    if (framesPerDice[i] > framesMax)
                        framesMax = framesPerDice[i];
                }
            }

            GUI.Label(new(20, 20, 800, 20), $"Buffers: {dices} x {(framesMin == framesMax ? framesMin : $"{framesMin}-{framesMax}")}");
        }

        struct SimulatedPhysicsFrame
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        class SimulatedPhysicsFramesData
        {
            public Queue<SimulatedPhysicsFrame> Frames = new();

            public Vector3 LastLinearVelocity;
            public Vector3 LastAngularVelocity;
            public bool WasSleepingAtTheEnd;
        }

        enum SimulationEndThreshold
        {
            AllSleeping,
            DiceSleeping,
            LowVelocity,
        }
    }
}

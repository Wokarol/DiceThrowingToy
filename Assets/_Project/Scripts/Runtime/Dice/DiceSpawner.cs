using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Wokarol.Common;
using Wokarol.GameSystemsLocator;
using Wokarol.Utils;
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
        [SerializeField] private bool keepRolledDiceDynamic = true;
        [Space]
        [SerializeField] private bool extraDebugData = false;

        List<Dice> allDynamicDice = new();
        List<Dice> diceRolledThisThrow = new();

        List<Queue<SimulatedPhysicsFrame>> computedAnimation = new();
        private bool isRollingDice;

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

            allDynamicDice.RemoveAll(d => d == null);

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

                allDynamicDice.Add(d);
                diceRolledThisThrow.Add(d);
                computedAnimation.Add(new());
            }

            if (Keyboard.current.ctrlKey.isPressed)
                Debug.Break();

            Physics.simulationMode = SimulationMode.Script;

            Physics.Simulate(Time.fixedDeltaTime);

            for (int step = 0; step < 1000; step++)
            {
                Physics.Simulate(Time.fixedDeltaTime);

                bool areAllDiceDone = true;

                for (int i = 0; i < allDynamicDice.Count; i++)
                {
                    var d = allDynamicDice[i];

                    if (!d.Body.IsSleeping())
                        areAllDiceDone = false;

                    computedAnimation[i].Enqueue(new()
                    {
                        Position = d.Body.position,
                        Rotation = d.Body.rotation,
                    });
                }

                if (areAllDiceDone)
                {
                    break;
                }
                else if (step == 999)
                {
                    Debug.Log($"{LogExtras.Prefix(this)} Reached the simulation step limit");
                }
            }

            var steps = computedAnimation[0].Count;

            // We prep the dice for the animation
            for (int i = 0; i < allDynamicDice.Count; i++)
            {
                var d = allDynamicDice[i];
                d.Body.isKinematic = true;
            }

            for (int i = 0; i < diceRolledThisThrow.Count; i++)
            {
                var d = diceRolledThisThrow[i];
                d.ForceValue(values[i]);
            }

            // We reset the dice
            for (int i = 0; i < allDynamicDice.Count; i++)
            {
                var frame = computedAnimation[i].Dequeue();

                allDynamicDice[i].transform.position = frame.Position;
                allDynamicDice[i].transform.rotation = frame.Rotation;

                allDynamicDice[i].Body.position = frame.Position;
                allDynamicDice[i].Body.rotation = frame.Rotation;
            }

            // The simulation starts again
            Physics.simulationMode = SimulationMode.FixedUpdate;

            for (int step = 0; step < (steps - 1); step++) // The first step is handled by reseting
            {
                await UniTask.WaitForFixedUpdate(ct);

                for (int i = 0; i < allDynamicDice.Count; i++)
                {
                    var frame = computedAnimation[i].Dequeue();

                    allDynamicDice[i].Body.position = frame.Position;
                    allDynamicDice[i].Body.rotation = frame.Rotation;
                }
            }

            // We clean up and revert the things to correct state
            if (keepRolledDiceDynamic)
            {
                foreach (var d in allDynamicDice)
                {
                    d.Body.isKinematic = false;
                }
            }
            else
            {
                allDynamicDice.Clear();
            }

            isRollingDice = false;
        }

        private void OnGUI()
        {
            if (!extraDebugData) return;

            int dices = 0;
            int framesMin = 0;
            int framesMax = 0;


            if (allDynamicDice.Count > 0)
            {
                Span<int> framesPerDice = stackalloc int[allDynamicDice.Count];
                dices = allDynamicDice.Count;

                for (int i = 0; i < allDynamicDice.Count; i++)
                {
                    framesPerDice[i] = computedAnimation[i]?.Count ?? 0;
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
    }
}

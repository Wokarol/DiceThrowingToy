using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

        List<Dice> diceMidThrow = new ();

        List<Queue<SimulatedPhysicsFrame>> computedAnimation = new ();
        private bool isRollingDice;

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                var isShiftDown = Keyboard.current.shiftKey.isPressed;

                RollDices(isShiftDown ? 1 : 5).Forget();
            }
        }

        private async UniTask RollDices(int amount)
        {
            if (isRollingDice) return; // Interlock

            isRollingDice = true;

            for (int i = 0; i < amount; i++)
            {
                var offset = Quaternion.AngleAxis((i / (float)amount) * 360f, transform.forward) * (transform.up * 1.5f);

                var d = Instantiate(dice, transform.position + offset, transform.rotation);

                var angle = Random.Range(-throwDirectionMaxAngle, throwDirectionMaxAngle);
                var force = Random.Range(minForce, maxForce);
                var torqueForce = Random.Range(minTorque, maxTorque);
                var torqueAxis = Random.onUnitSphere;

                d.Body.AddForce(force * (Quaternion.AngleAxis(angle, Vector3.up) * transform.forward), ForceMode.Impulse);
                d.Body.AddTorque(torqueForce * torqueAxis, ForceMode.Impulse);

                diceMidThrow.Add(d);
                computedAnimation.Add(new());
            }

            if (Keyboard.current.ctrlKey.isPressed)
                Debug.Break();

            Physics.simulationMode = SimulationMode.Script;

            Physics.Simulate(Time.fixedDeltaTime);

            foreach (var d in diceMidThrow)
            {
                Debug.Log(d.Body.linearVelocity);
            }


            for (int step = 0; step < 1000; step++)
            {
                Physics.Simulate(Time.fixedDeltaTime);

                bool areAllDiceDone = true;

                for (int i = 0; i < diceMidThrow.Count; i++)
                {
                    var d = diceMidThrow[i];

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
                    Debug.Log("They slept");
                    break;
                }
                else if (step == 1000)
                {
                    Debug.Log("They never slept");
                }
            }

            var steps = computedAnimation[0].Count;

            // We prep the dice for the animation
            foreach (var d in diceMidThrow)
            {
                d.Body.isKinematic = true;
            }

            // We reset the dice
            for (int i = 0; i < diceMidThrow.Count; i++)
            {
                var frame = computedAnimation[i].Dequeue();

                diceMidThrow[i].transform.position = frame.Position;
                diceMidThrow[i].transform.rotation = frame.Rotation;

                diceMidThrow[i].Body.position = frame.Position;
                diceMidThrow[i].Body.rotation = frame.Rotation;
            }

            // The simulation starts again
            Physics.simulationMode = SimulationMode.FixedUpdate;

            for (int step = 0; step < (steps - 1); step++) // The first step is handled by reseting
            {
                await UniTask.WaitForFixedUpdate();

                for (int i = 0; i < diceMidThrow.Count; i++)
                {
                    var frame = computedAnimation[i].Dequeue();

                    diceMidThrow[i].Body.position = frame.Position;
                    diceMidThrow[i].Body.rotation = frame.Rotation;
                }
            }

            // We clean up and revert the things to correct state
            if (keepRolledDiceDynamic)
            {
                foreach (var d in diceMidThrow)
                {
                    d.Body.isKinematic = false;
                }
            }
            else
            {
                diceMidThrow.Clear();
            }

            isRollingDice = false;
        }

        private void OnGUI()
        {
            if (!extraDebugData) return;

            int dices = 0;
            int framesMin = 0;
            int framesMax = 0;


            if (diceMidThrow.Count > 0)
            {
                Span<int> framesPerDice = stackalloc int[diceMidThrow.Count];
                dices = diceMidThrow.Count;

                for (int i = 0; i < diceMidThrow.Count; i++)
                {
                    framesPerDice[i] = computedAnimation[i]?.Count ?? 0;
                }

                framesMin = 9999999;
                framesMax = 0;

                for (int i = 0; i < framesPerDice.Length; i++)
                {
                    if (framesPerDice[i] <  framesMin)
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

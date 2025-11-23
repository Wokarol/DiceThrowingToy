using UnityEngine;

namespace Wokarol
{
    public class D12Dice : Dice
    {
        const float phi = 1.61803400516510009765625f;

        public override int FaceCount => 12;
        public override int FaceEdgeCount => 5;

        public override (Vector3 normal, Vector3 forward) GetLocalFaceCoordinates(int faceIndex)
        {
            if (faceIndex < 0 || faceIndex >= FaceCount) throw new System.ArgumentOutOfRangeException(nameof(faceIndex));

            var (normal, sideAxis) = faceIndex switch
            {
                0 => (new Vector3(0, 1, phi).normalized, -Vector3.right),
                1 => (new Vector3(0, 1, -phi).normalized, Vector3.right),
                2 => (new Vector3(0, -1, phi).normalized, Vector3.right),
                3 => (new Vector3(0, -1, -phi).normalized, -Vector3.right),

                4 => (new Vector3(1, phi, 0).normalized, -Vector3.forward),
                5 => (new Vector3(1, -phi, 0).normalized, Vector3.forward),
                6 => (new Vector3(-1, phi, 0).normalized, Vector3.forward),
                7 => (new Vector3(-1, -phi, 0).normalized, -Vector3.forward),

                8 => (new Vector3(phi, 0, 1).normalized, -Vector3.up),
                9 => (new Vector3(phi, 0, -1).normalized, Vector3.up),
                10 => (new Vector3(-phi, 0, 1).normalized, Vector3.up),
                11 => (new Vector3(-phi, 0, -1).normalized, -Vector3.up),

                _ => default,
            };

            return (normal, Vector3.Cross(normal, sideAxis));
        }
    }
}

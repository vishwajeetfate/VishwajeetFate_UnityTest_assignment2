using UnityEngine;

public class BallBounceSpin : MonoBehaviour
{
    private bool hasBounced = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasBounced) return;

        // Detect bounce on pitch or low point of contact
        if (collision.gameObject.CompareTag("Pitch") || collision.contacts[0].point.y < transform.position.y - 0.1f)
        {
            hasBounced = true;
            BallThrower.instance.ballHasBounced = true;

            Debug.Log("Ball has bounced!");

            Rigidbody rb = GetComponent<Rigidbody>();

            if (BallThrower.instance.isSpin)
            {
                // Determine spin direction based on type and bowling arm
                Vector3 baseForward = BallThrower.instance.transform.forward;
                Vector3 turnDir = Vector3.Cross(Vector3.up, baseForward).normalized;

                // OffSpin = turn into batter (Right arm = left turn), LegSpin = turn away
                bool isOffSpin = BallThrower.instance.spinType == SpinType.OffSpin;
                bool isLeftArm = BallThrower.instance.isLeftArm;

                if (isOffSpin ^ isLeftArm)  // XOR logic for turning side
                    turnDir = -turnDir;

                // Apply side force and reduce speed slightly to simulate friction loss
                float spinStrength = BallThrower.instance.spinStrength;
                float turnAmount = spinStrength * 3f; // tuned for impact

                Debug.Log($"Applying turn: {turnDir} x {turnAmount}");

                // Apply side velocity and slow down a bit
                rb.velocity += turnDir * turnAmount;
                rb.velocity *= 0.95f;

                // Optional: Add rotational torque for visual realism
                rb.AddTorque(turnDir * spinStrength * 20f, ForceMode.Impulse);
            }
        }
    }
}

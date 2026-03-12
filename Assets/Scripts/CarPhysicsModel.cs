using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarPhysicsModel : MonoBehaviour
{
    public enum SurfaceType
    {
        Asphalt,
        Mud,
        Ice
    }

    readonly struct SurfaceCoefficients
    {
        public readonly float ForwardGrip;
        public readonly float LateralGrip;
        public readonly float RollingResistance;
        public readonly float LinearDrag;

        public SurfaceCoefficients(float forwardGrip, float lateralGrip, float rollingResistance, float linearDrag)
        {
            ForwardGrip = forwardGrip;
            LateralGrip = lateralGrip;
            RollingResistance = rollingResistance;
            LinearDrag = linearDrag;
        }
    }

    public Rigidbody rb;

    [Header("Vehicle")]
    public float wheelbase = 1.9f;
    public float maxSteeringAngle = 35f;
    public float maxEngineForce = 6000f;
    public float maxBrakeForce = 8000f;
    public float steeringResponse = 2200f;
    public float yawDamping = 320f;
    public float lateralDamping = 6f;

    [Header("Surface")]
    public SurfaceType surfaceType = SurfaceType.Asphalt;

    static readonly SurfaceCoefficients Asphalt = new(1.0f, 1.0f, 0.015f, 0.06f);
    static readonly SurfaceCoefficients Mud = new(0.55f, 0.6f, 0.06f, 0.08f);
    static readonly SurfaceCoefficients Ice = new(0.2f, 0.25f, 0.005f, 0.03f);

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        ApplyLowFrictionContactMaterial();
    }

    public void Step(float desiredAcceleration, float desiredSteeringAngleDeg)
    {
        if (rb == null)
            return;

        SurfaceCoefficients surface = GetSurface(surfaceType);
        Vector3 gravity = Physics.gravity;
        float gravityMagnitude = Mathf.Max(0.1f, gravity.magnitude);

        Vector3 planarVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
        Vector3 localVelocity = transform.InverseTransformDirection(planarVelocity);
        float forwardSpeed = localVelocity.z;
        float lateralSpeed = localVelocity.x;

        float requestedForce = desiredAcceleration * rb.mass;
        if (Mathf.Abs(requestedForce) < 5f)
            requestedForce = 0f;

        float maxTractionForce = surface.ForwardGrip * rb.mass * gravityMagnitude;
        float maxForwardForce = Mathf.Min(maxEngineForce, maxTractionForce);
        float maxBrakeForceLimited = Mathf.Min(maxBrakeForce, maxTractionForce);

        float longitudinalForce;
        if (requestedForce >= 0f)
        {
            longitudinalForce = Mathf.Clamp(requestedForce, 0f, maxForwardForce);
        }
        else
        {
            float brakeMagnitude = Mathf.Clamp(-requestedForce, 0f, maxBrakeForceLimited);

            if (Mathf.Abs(forwardSpeed) < 0.05f)
            {
                // Prevent "braking" from creating reverse motion when nearly stopped.
                longitudinalForce = 0f;
            }
            else
            {
                longitudinalForce = -Mathf.Sign(forwardSpeed) * brakeMagnitude;
            }
        }

        rb.AddForce(transform.forward * longitudinalForce, ForceMode.Force);

        float maxLateralForce = surface.LateralGrip * rb.mass * gravityMagnitude;
        float lateralForce = -lateralSpeed * lateralDamping * surface.LateralGrip * rb.mass;
        lateralForce = Mathf.Clamp(lateralForce, -maxLateralForce, maxLateralForce);
        rb.AddForce(transform.right * lateralForce, ForceMode.Force);

        if (planarVelocity.sqrMagnitude > 0.001f)
        {
            Vector3 dragForce = -planarVelocity * surface.LinearDrag;
            rb.AddForce(dragForce, ForceMode.Force);

            Vector3 rollingForce = -planarVelocity.normalized * (surface.RollingResistance * rb.mass * gravityMagnitude);
            rb.AddForce(rollingForce, ForceMode.Force);
        }

        float clampedSteeringDeg = Mathf.Clamp(desiredSteeringAngleDeg, -maxSteeringAngle, maxSteeringAngle);
        float steeringRad = clampedSteeringDeg * Mathf.Deg2Rad;
        float desiredYawRate = Mathf.Abs(wheelbase) > 0.001f
            ? (Mathf.Max(0f, forwardSpeed) / wheelbase) * Mathf.Tan(steeringRad)
            : 0f;
        float yawRateError = desiredYawRate - rb.angularVelocity.y;
        float yawTorque = yawRateError * steeringResponse - rb.angularVelocity.y * yawDamping;
        rb.AddTorque(Vector3.up * yawTorque, ForceMode.Force);
    }

    static SurfaceCoefficients GetSurface(SurfaceType surface)
    {
        return surface switch
        {
            SurfaceType.Mud => Mud,
            SurfaceType.Ice => Ice,
            _ => Asphalt,
        };
    }

    void ApplyLowFrictionContactMaterial()
    {
        PhysicsMaterial lowFriction = new PhysicsMaterial("CarLowFriction")
        {
            staticFriction = 0.01f,
            dynamicFriction = 0.01f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = 0f,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            if (col != null)
                col.sharedMaterial = lowFriction;
        }
    }
}
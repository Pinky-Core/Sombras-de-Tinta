using UnityEngine;

public class SideFollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2.5f, -10f);
    public float smooth = 10f;

    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, offset.z);
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation((target.position + Vector3.up * 1.2f) - transform.position, Vector3.up), 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }
}

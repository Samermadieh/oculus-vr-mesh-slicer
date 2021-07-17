using UnityEngine;

namespace Assets.Scripts
{
    public class Slice : MonoBehaviour
    {
        public GameObject tip;
        public GameObject bottom;
        Vector3 entryTipPos;
        Vector3 entryBottomPos;
        Vector3 exitTipPos;
        bool hasEntered;

        private void OnTriggerEnter(Collider other)
        {
            GameObject obj = other.gameObject;
            if (obj.GetComponent<Sliceable>() != null)
            {
                hasEntered = true;
                entryTipPos = tip.transform.position;
                entryBottomPos = bottom.transform.position;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (hasEntered && other.gameObject.GetComponent<Sliceable>() != null)
            {
                exitTipPos = tip.transform.position;
                SliceObject(other.gameObject);
            }
        }

        void SliceObject(GameObject obj)
        {
            // Create Triangle between tip and bottom of saw to get the normal.
            Vector3 side1 = exitTipPos - entryTipPos;
            Vector3 side2 = exitTipPos - entryBottomPos;

            // Get the perpendicular point
            Vector3 normal = Vector3.Cross(side1, side2).normalized;

            Vector3 tNormal = ((Vector3)(obj.transform.localToWorldMatrix.transpose * normal)).normalized;

            Vector3 tStartPoint = obj.transform.InverseTransformPoint(entryTipPos);

            Plane plane = new Plane();
            plane.SetNormalAndPosition(tNormal, tStartPoint);

            if (tNormal.x < 0 || tNormal.y < 0)
            {
                plane = plane.flipped;
            }

            GameObject[] slices = SliceIt(plane, obj);

            Rigidbody rb = slices[1].GetComponent<Rigidbody>();
            Vector3 newNormal = tNormal + Vector3.up * 1f;
            rb.AddForce(newNormal, ForceMode.Impulse);
        }


        // Slice Into Two Objects Method
        public static GameObject[] SliceIt(Plane plane, GameObject objectToCut)
        {
            Mesh mesh = objectToCut.GetComponent<MeshFilter>().mesh;
            var a = mesh.GetSubMesh(0);
            Sliceable sliceable = objectToCut.GetComponent<Sliceable>();

            SliceData slicesData = new SliceData(plane, mesh, sliceable.IsSolid, sliceable.ShareVertices, sliceable.SmoothVertices);

            GameObject positiveObject = MakeObject(objectToCut);
            positiveObject.name = string.Format("{0}_positive", objectToCut.name);

            GameObject negativeObject = MakeObject(objectToCut);
            negativeObject.name = string.Format("{0}_negative", objectToCut.name);

            var positiveSideMeshData = slicesData.PositiveSideMesh;
            var negativeSideMeshData = slicesData.NegativeSideMesh;

            positiveObject.GetComponent<MeshFilter>().mesh = positiveSideMeshData;
            negativeObject.GetComponent<MeshFilter>().mesh = negativeSideMeshData;

            SetupCollidersAndRigidBodys(ref positiveObject, positiveSideMeshData, sliceable.UseGravity);
            SetupCollidersAndRigidBodys(ref negativeObject, negativeSideMeshData, sliceable.UseGravity);

            Destroy(objectToCut);

            return new GameObject[] { positiveObject, negativeObject };
        }


        private static GameObject MakeObject(GameObject originalObject)
        {
            var mat = originalObject.GetComponent<MeshRenderer>().materials;

            GameObject meshObject = new GameObject();
            Sliceable oSliceable = originalObject.GetComponent<Sliceable>();

            meshObject.AddComponent<MeshFilter>();
            meshObject.AddComponent<MeshRenderer>();
            Sliceable sliceable = meshObject.AddComponent<Sliceable>();

            sliceable.IsSolid = oSliceable.IsSolid;
            sliceable.UseGravity = oSliceable.UseGravity;

            meshObject.GetComponent<MeshRenderer>().materials = mat;

            meshObject.transform.localScale = originalObject.transform.localScale;
            meshObject.transform.rotation = originalObject.transform.rotation;
            meshObject.transform.position = originalObject.transform.position;

            meshObject.tag = originalObject.tag;

            return meshObject;
        }


        private static void SetupCollidersAndRigidBodys(ref GameObject gameObject, Mesh mesh, bool useGravity)
        {
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = useGravity;
        }
    }
}
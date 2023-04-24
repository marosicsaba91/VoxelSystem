using UnityEngine;

public class NormalDrawer : MonoBehaviour
{
	enum WhenToDraw
	{
		Always,
		Selected
	}

	[SerializeField] MeshFilter meshFilter;
	[SerializeField] WhenToDraw whenToDraw = WhenToDraw.Selected;
	[SerializeField, Min(1)] int maxLineCount = 1000;
	[SerializeField] float normalLength = 0.2f;

	[Header("Vertex")]
	[SerializeField]
	bool drawVertexNormals = true;
	[SerializeField, Min(1)] int everyNthVertex = 1;
	[SerializeField, Min(0)] int startAtVertex = 0;
	[SerializeField, Min(-1)] int endAtVertex = -1;
	[Space]
	[SerializeField] Color vertexNormalColor = Color.yellow;

	[Header("Triangle")]
	[SerializeField] bool drawTriangles = true;
	[SerializeField, Min(1)] int everyNthTriangle = 1;
	[SerializeField, Min(0)] int startAtTriangle = 0;
	[SerializeField, Min(-1)] int endAtTriangle = -1;
	[Space]
	[SerializeField] bool drawOutline;
	[SerializeField] Color outlineColor = new(0, 0, 0, 0.25f);
	[Space]
	[SerializeField] bool drawMeanVertexNormals;
	[SerializeField] Color rightMeanVertexNormalsColor = Color.green;
	[SerializeField] Color wrongMeanVertexNormalsColor = Color.red;
	[Space]
	[SerializeField] bool drawWindings;
	[SerializeField] bool drawNormalsFromWinding;
	[SerializeField] Color windingColor = Color.cyan;


	void OnValidate()
	{
		if (meshFilter == null)
			meshFilter = GetComponent<MeshFilter>();
	}

	void OnDrawGizmosSelected()
	{
		if (whenToDraw == WhenToDraw.Selected)
			DrawGizmos();
	}

	void OnDrawGizmos()
	{
		if (whenToDraw == WhenToDraw.Always)
			DrawGizmos();
	}

	int _allIneCount = 0;

	void DrawGizmos()
	{
		Matrix4x4 oldMatrix = Gizmos.matrix;
		Gizmos.matrix = transform.localToWorldMatrix;

		Mesh mesh = meshFilter.sharedMesh;
		_allIneCount = 0;
		DrawVertexNormals(mesh);
		DrawTriangles(mesh);

		Gizmos.matrix = oldMatrix;
	}

	void DrawTriangles(Mesh mesh)
	{
		if (mesh == null || mesh.triangles == null)
			return;
		if (!drawTriangles)
			return;
		if (!drawNormalsFromWinding && !drawMeanVertexNormals && !drawWindings && !drawOutline)
			return;

		int endIndex = endAtTriangle < startAtTriangle ? mesh.triangles.Length : Mathf.Min(endAtTriangle * 3, mesh.triangles.Length);
		for (int i = startAtTriangle * 3; i < endIndex; i += 3 * everyNthTriangle)
		{
			Vector3 p0 = mesh.vertices[mesh.triangles[i]];
			Vector3 p1 = mesh.vertices[mesh.triangles[i + 1]];
			Vector3 p2 = mesh.vertices[mesh.triangles[i + 2]];
			Vector3 center = (p0 + p1 + p2) / 3f;
			Vector3 sideNormal = Vector3.Cross(p1 - p0, p2 - p0).normalized;

			if (drawOutline)
			{
				Gizmos.color = outlineColor;
				Gizmos.DrawLine(p0, p1);
				Gizmos.DrawLine(p1, p2);
				Gizmos.DrawLine(p2, p0);
				_allIneCount += 3;
			}

			if (drawWindings)
			{
				Gizmos.color = windingColor;
				Vector3 wp0 = Vector3.Lerp(p0, center, 0.25f);
				Vector3 wp1 = Vector3.Lerp(p1, center, 0.25f);
				Vector3 wp2 = Vector3.Lerp(p2, center, 0.25f);
				Vector3 wp3 = Vector3.Lerp(wp2, center, 0.5f);
				Gizmos.DrawLine(wp0, wp1);
				Gizmos.DrawLine(wp1, wp2);
				Gizmos.DrawLine(wp2, wp3);

				_allIneCount += 3;
			}

			if (drawNormalsFromWinding)
			{
				Gizmos.color = windingColor;
				Gizmos.DrawLine(center, center + sideNormal * normalLength);
				_allIneCount++;
			}

			if (drawMeanVertexNormals)
			{
				Vector3 meanNormal = (mesh.normals[mesh.triangles[i]] + mesh.normals[mesh.triangles[i + 1]] +
									  mesh.normals[mesh.triangles[i + 2]]) / 3f;
				float angle = Vector3.Angle(meanNormal, sideNormal);
				Gizmos.color = angle > 90f ? wrongMeanVertexNormalsColor : rightMeanVertexNormalsColor;
				Gizmos.DrawLine(center, center + meanNormal * normalLength);
				_allIneCount++;
			}

			if (_allIneCount > maxLineCount)
				return;
		}
	}

	void DrawVertexNormals(Mesh mesh)
	{
		if (mesh == null || mesh.vertices == null || mesh.normals == null)
			return;
		if (!drawVertexNormals)
			return;

		int endIndex = endAtVertex < startAtVertex ? mesh.vertices.Length : Mathf.Min(endAtVertex, mesh.vertices.Length);
		for (int i = startAtVertex; i < endIndex; i += everyNthVertex)
		{
			Vector3 vertex = mesh.vertices[i];
			Vector3 normal = mesh.normals[i];
			Gizmos.color = vertexNormalColor;
			Gizmos.DrawLine(vertex, vertex + normal * normalLength);
			_allIneCount++;
			if (_allIneCount > maxLineCount)
				return;
		}
	}
}
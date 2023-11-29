using MUtility;
using System;
using UnityEngine;

namespace VoxelSystem
{

	class IntVoxelTester : MonoBehaviour
	{
		enum InputSource { Decimal, Binary, Composite, None }

		[SerializeField] InputSource inputSource;
		[Space]
		[SerializeField] int decimalValue = 0;
		[Space]
		[SerializeField] string binaryValue;
		[Space]
		[SerializeField] Vector3Int rotation;
		[SerializeField] Flip3D fliping;
		
		[SerializeField] byte shapeIndex;
		[SerializeField] byte materialIndex;

		[SerializeField] ushort extraData;

		void OnValidate()
		{
			switch (inputSource)
			{
				case InputSource.Decimal:
					SetValues(decimalValue);
					break;
				case InputSource.Binary:
					SetValues(GetDecimalFromBinary(binaryValue));
					break;
				case InputSource.Composite:
					SetValues(GetDecimalFromComposite());
					break;
			}
		}

		int GetDecimalFromComposite()
		{
			int value = 0;
			 
			value.SetMaterialIndex(materialIndex);
			value.SetShapeIndex(shapeIndex);
			ushort extraData = value.GetExtraVoxelData();
			extraData.SetFlip(fliping);
			extraData.SetRotation(rotation.x, rotation.y, rotation.z); 
			value.SetExtraVoxelData(extraData);

			return value;
		}

		int GetDecimalFromBinary(string binaryValue)
		{
			binaryValue = binaryValue.Replace(" ", "");
			binaryValue = binaryValue.Replace("\t", "");

			//Replace every character that is not 0 or 1 with 0
			binaryValue = System.Text.RegularExpressions.Regex.Replace(binaryValue, "[^0-1]", "0");

			// Add 0s to the right until the string is 32 characters long
			binaryValue = binaryValue.PadRight(32, '0');

			// Cut off the first 32 characters
			binaryValue = binaryValue[..32]; 

			//Get the decimal value
			return Convert.ToInt32(binaryValue, 2);
		}

		void SetValues(int value)
		{
			decimalValue = value;

			binaryValue = Convert.ToString(value, 2).PadLeft(32, '0');
			binaryValue = System.Text.RegularExpressions.Regex.Replace(binaryValue, ".{8}", "$0\t");

			materialIndex = value.GetMaterialIndex();
			shapeIndex = value.GetShapeIndex();
			extraData = value.GetExtraVoxelData();
			fliping = extraData.GetFlip();
			rotation = extraData.GetRotation();
		}
	}
}
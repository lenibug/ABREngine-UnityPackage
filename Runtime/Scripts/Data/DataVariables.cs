/* DataVariables.cs
 *
 * Copyright (c) 2021 University of Minnesota
 * Authors: Bridger Herman <herma582@umn.edu>, Seth Johnson <sethalanjohnson@gmail.com>
 *
 */

using UnityEngine;

namespace IVLab.ABREngine
{
    public interface IDataVariable<T> : IHasDataset, IABRInput
    {
        /// <summary>
        ///     The DataPath that represents this variable
        /// </summary>
        string Path { get; }

        /// <summary>
        ///     MinValue is calculated by the DataManager when it imports a new
        ///     dataset. MinValue is the smallest value encountered across every
        ///     instance of this variable, across all datasets.
        /// </summary>
        T MinValue { get; set; }

        /// <summary>
        ///     MaxValue is calculated by the DataManager when it imports a new
        ///     dataset. MaxValue is the largest value encountered across every
        ///     instance of this variable, across all datasets.
        /// </summary>
        T MaxValue { get; set; }

        /// <summary>
        ///     Get the actual data values in the context of this particular Key
        ///     Data object
        /// </summary>
        T[] GetArray(IKeyData keyData);

        /// <summary>
        ///     Determine if this variable is a part of the key data
        /// </summary>
        bool IsPartOf(IKeyData keyData);
    }

    public class ScalarDataVariable : IDataVariable<float>, IHasDataset
    {
        public ABRInputGenre Genre { get; } = ABRInputGenre.Variable;
        public string Path { get; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }

        public ScalarDataVariable(string path)
        {
            Path = path;
        }

        public bool IsPartOf(IKeyData keyData)
        {
            // Get the actual name of this variable
            string varName = DataPath.GetName(Path);

            // Get the raw dataset
            RawDataset dataset;
            ABREngine.Instance.Data.TryGetRawDataset(keyData.Path, out dataset);

            return dataset.HasScalarArray(varName);
        }

        public float[] GetArray(IKeyData keyData) {
            // Get the actual name of this variable
            string varName = DataPath.GetName(Path);

            // Get the raw dataset
            RawDataset dataset;
            ABREngine.Instance.Data.TryGetRawDataset(keyData.Path, out dataset);

            // Return the scalar array
            return dataset?.GetScalarArray(varName);
        }

        public Dataset GetDataset()
        {
            string datasetPath = DataPath.GetDatasetPath(Path);
            Dataset dataset;
            ABREngine.Instance.Data.TryGetDataset(datasetPath, out dataset);
            return dataset;
        }
    }

    public class VectorDataVariable : IDataVariable<Vector3>, IHasDataset
    {
        public ABRInputGenre Genre { get; } = ABRInputGenre.Variable;
        public string Path { get; }
        public Vector3 MinValue { get; set; }
        public Vector3 MaxValue { get; set; }

        public VectorDataVariable(string path)
        {
            Path = path;
        }

        public bool IsPartOf(IKeyData keyData)
        {
            // Get the actual name of this variable
            string varName = DataPath.GetName(Path);

            // Get the raw dataset
            RawDataset dataset;
            ABREngine.Instance.Data.TryGetRawDataset(keyData.Path, out dataset);

            return dataset.HasVectorArray(varName);
        }

        public Vector3[] GetArray(IKeyData keyData) {
            // Get the actual name of this variable
            string varName = DataPath.GetName(Path);

            // Get the raw dataset
            RawDataset dataset;
            ABREngine.Instance.Data.TryGetRawDataset(keyData.Path, out dataset);

            // Return the vector array
            return dataset?.GetVectorArray(varName);
        }

        public Dataset GetDataset()
        {
            string datasetPath = DataPath.GetDatasetPath(Path);
            Dataset dataset;
            ABREngine.Instance.Data.TryGetDataset(datasetPath, out dataset);
            return dataset;
        }
    }
}
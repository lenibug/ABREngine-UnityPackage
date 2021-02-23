/* DataImpression.cs
 *
 * Copyright (c) 2021 University of Minnesota
 * Authors: Bridger Herman <herma582@umn.edu>, Seth Johnson <sethalanjohnson@gmail.com>
 *
 */

using System;
using UnityEngine;

namespace IVLab.ABREngine
{
    /// <summary>
    ///     Public interface for a single ABR visualization layer
    /// </summary>
    public interface IDataImpression : IHasDataset
    {
        /// <summary>
        ///     Unique identifier for this Data Impression
        ///
        ///     Assigned on object creation
        /// </summary>
        Guid Uuid { get; }

        /// <summary>
        ///     Used for getting/setting ABRInputs on this DataImpression
        /// </summary>
        ABRInputIndexerModule InputIndexer { get; }

        /// <summary>
        ///     Performs any pre-calculations necessary to render this
        ///     particular type of Key Data (for instance, the individual glyph
        ///     positions for the InstanceMeshRenderer used in glyph rendering)
        ///
        ///     Note: `ComputeKeyDataRenderInfo()`, `ComputeRenderInfo()`, and
        ///     `ApplyToGameObject()` should be run in sequence.
        /// </summary>
        void ComputeKeyDataRenderInfo();

        /// <summary>
        ///     Populates rendering information (Geometry) for the
        ///     DataImpression
        ///
        ///     Note: `ComputeKeyDataRenderInfo()`, `ComputeRenderInfo()`, and
        ///     `ApplyToGameObject()` should be run in sequence.
        /// </summary>
        void ComputeRenderInfo();

        /// <summary>
        ///     Applies a DataImpression to a particular GameObject
        ///
        ///     Note: `ComputeKeyDataRenderInfo()`, `ComputeRenderInfo()`, and
        ///     `ApplyToGameObject()` should be run in sequence.
        /// </summary>
        void ApplyToGameObject(EncodedGameObject currentGameObject);
    }

    /// <summary>
    ///     Private data for a single data impression
    ///
    ///     Should contain properties with attributes for all of the inputs
    /// </summary>
    public abstract class DataImpression : IDataImpression, IHasDataset
    {
        public Guid Uuid { get; }

        public ABRInputIndexerModule InputIndexer { get; }

        /// <summary>
        ///     Name of the material to use to render this DataImpression
        /// </summary>
        protected virtual string MaterialName { get; }

        /// <summary>
        ///     Slot to load the material into at runtime
        /// </summary>
        protected virtual Material ImpressionMaterial { get; }

        /// <summary>
        ///     Storage for the rendering data to be sent to the shader
        /// </summary>
        protected virtual MaterialPropertyBlock MatPropBlock { get; set; }

        /// <summary>
        ///     Cache of current rendering information
        /// </summary>
        protected virtual IDataImpressionRenderInfo RenderInfo { get; set; }

        /// <summary>
        ///     Cache of current KeyData rendering information
        /// </summary>
        protected virtual IKeyDataRenderInfo KeyDataRenderInfo { get; set; }

        /// <summary>
        ///     The layer to put this data impression in
        ///
        ///     Warning: layer must exist in the Unity project!
        /// </summary>
        protected virtual string LayerName { get; } = "ABR";

        /// <summary>
        ///     Construct a data impession with a given UUID. Note that this
        ///     will be called from ABRState and must assume that there's a
        ///     single string argument with UUID - if you override this
        ///     constructor bad things might happen.
        /// </summary>
        public DataImpression(string uuid)
        {
            InputIndexer = new ABRInputIndexerModule(this);
            Uuid = new Guid(uuid);
            MatPropBlock = new MaterialPropertyBlock();
            ImpressionMaterial = Resources.Load<Material>(MaterialName);
            if (ImpressionMaterial == null)
            {
                Debug.LogWarningFormat("Material `{0}` not found for {1}", MaterialName, this.GetType().ToString());
            }
        }

        public DataImpression() : this(Guid.NewGuid().ToString()) { }

        public virtual void ComputeKeyDataRenderInfo() { }

        public virtual void ComputeRenderInfo() { }

        public virtual void ApplyToGameObject(EncodedGameObject currentGameObject) { }

        /// <summary>
        ///     By default, there's no dataset. DataImpressions should only have
        ///     one dataset, and it's up to them individually to enforce that
        ///     they correctly implement this.
        /// </summary>
        public virtual Dataset GetDataset()
        {
            return null;
        }
    }


    public interface IDataImpressionRenderInfo { }
}
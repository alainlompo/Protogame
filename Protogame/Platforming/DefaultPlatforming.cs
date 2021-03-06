using Microsoft.Xna.Framework;

namespace Protogame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The default platforming.
    /// </summary>
    internal class DefaultPlatforming : IPlatforming
    {
        private readonly ITransformUtilities _transformUtilities;
        
        private readonly IBoundingBoxUtilities _boundingBoxUtilities;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPlatforming"/> class.
        /// </summary>
        /// <param name="transformUtilities">
        /// The transform utilities.
        /// </param>
        /// <param name="boundingBoxUtilities">
        /// The bounding box utilities.
        /// </param>
        public DefaultPlatforming(
            ITransformUtilities transformUtilities,
            IBoundingBoxUtilities boundingBoxUtilities)
        {
            _transformUtilities = transformUtilities;
            _boundingBoxUtilities = boundingBoxUtilities;
        }

        /// <summary>
        /// The apply action until.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <param name="action">
        /// The action.
        /// </param>
        /// <param name="check">
        /// The check.
        /// </param>
        /// <param name="maximum">
        /// The maximum.
        /// </param>
        public void ApplyActionUntil(
            IBoundingBox entity, 
            Action<IBoundingBox> action, 
            Func<IBoundingBox, bool> check, 
            int? maximum)
        {
            if (maximum == null)
            {
                while (!check(entity))
                {
                    action(entity);
                }
            }
            else
            {
                for (var i = 0; i < maximum.Value; i++)
                {
                    if (!check(entity))
                    {
                        action(entity);
                    }
                }
            }
        }

        /// <summary>
        /// The apply gravity.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <param name="xGravity">
        /// The x gravity.
        /// </param>
        /// <param name="yGravity">
        /// The y gravity.
        /// </param>
        public void ApplyGravity(IHasVelocity entity, float xGravity, float yGravity)
        {
            entity.XSpeed += xGravity;
            entity.YSpeed += yGravity;
        }

        public bool ApplyOverheadCheck(IBoundingBox entity, IEnumerable<IBoundingBox> entities, Func<IBoundingBox, bool> ground, float yGravity, bool bounce)
        {
            var entityExtended = new BoundingBox
            {
                Width = entity.Width,
                Height = entity.Height,
                XSpeed = 0,
                YSpeed = 0
            };
            entityExtended.Transform.Assign(_transformUtilities.CreateFromModifiedSRTFinalTransform(
                entity.FinalTransform,
                Vector3.One,
                Quaternion.Identity,
                new Vector3(0, -1, 0)));
            var collidableEntities = entities.Where(ground).Where(x => x != entity).ToArray();
            foreach (var collidableEntity in collidableEntities)
            {
                if (this._boundingBoxUtilities.Overlaps(entityExtended, collidableEntity))
                {
                    if (bounce)
                    {
                        if (yGravity > 0)
                        {
                            entity.YSpeed = Math.Abs(entity.YSpeed);
                        }
                        else
                        {
                            entity.YSpeed = -Math.Abs(entity.YSpeed);
                        }
                    }
                    else
                    {
                        entity.YSpeed = 1;
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The apply movement.
        /// </summary>
        /// <param name="entity">
        ///     The entity.
        /// </param>
        /// <param name="xAmount">
        ///     The x amount.
        /// </param>
        /// <param name="yAmount">
        ///     The y amount.
        /// </param>
        /// <param name="entities">
        ///     The entities.
        /// </param>
        /// <param name="ground">
        ///     The ground.
        /// </param>
        public bool ApplyMovement(IBoundingBox entity, int xAmount, int yAmount, IEnumerable<IBoundingBox> entities, Func<IBoundingBox, bool> ground)
        {
            var movedX = false;
            var movedY = false;

            var collidableEntities = entities.Where(ground).ToArray();
            for (var x = 0; x < Math.Abs(xAmount); x++)
            {
                entity.Transform.LocalPosition += new Vector3(Math.Sign(xAmount), 0, 0);
                movedX = true;
                foreach (var other in collidableEntities)
                {
                    if (this._boundingBoxUtilities.Overlaps(entity, other))
                    {
                        entity.Transform.LocalPosition += new Vector3(-Math.Sign(xAmount), 0, 0);
                        movedX = false;
                        goto endX;
                    }
                }
            }

            endX:
            for (var y = 0; y < Math.Abs(yAmount); y++)
            {
                entity.Transform.LocalPosition += new Vector3(0, Math.Sign(yAmount), 0);
                movedY = true;
                foreach (var other in collidableEntities)
                {
                    if (this._boundingBoxUtilities.Overlaps(entity, other))
                    {
                        entity.Transform.LocalPosition += new Vector3(0, -Math.Sign(yAmount), 0);
                        movedY = false;
                        goto endY;
                    }
                }
            }

            endY:
            return movedX || movedY;
        }

        /// <summary>
        /// The clamp speed.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <param name="maximumXSpeed">
        /// The maximum x speed.
        /// </param>
        /// <param name="maximumYSpeed">
        /// The maximum y speed.
        /// </param>
        public void ClampSpeed(IHasVelocity entity, float? maximumXSpeed, float? maximumYSpeed)
        {
            if (maximumXSpeed != null)
            {
                if (entity.XSpeed < -Math.Abs(maximumXSpeed.Value))
                {
                    entity.XSpeed = -Math.Abs(maximumXSpeed.Value);
                }

                if (entity.XSpeed > Math.Abs(maximumXSpeed.Value))
                {
                    entity.XSpeed = Math.Abs(maximumXSpeed.Value);
                }
            }

            if (maximumYSpeed != null)
            {
                if (entity.YSpeed < -Math.Abs(maximumYSpeed.Value))
                {
                    entity.YSpeed = -Math.Abs(maximumYSpeed.Value);
                }

                if (entity.YSpeed > Math.Abs(maximumYSpeed.Value))
                {
                    entity.YSpeed = Math.Abs(maximumYSpeed.Value);
                }
            }
        }

        /// <summary>
        /// The is on ground.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <param name="entities">
        /// The entities.
        /// </param>
        /// <param name="ground">
        /// The ground.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsOnGround(IBoundingBox entity, IEnumerable<IBoundingBox> entities, Func<IBoundingBox, bool> ground)
        {
            var entityExtended = new BoundingBox
            {                
                Width = entity.Width, 
                Height = entity.Height, 
                XSpeed = entity.XSpeed, 
                YSpeed = entity.YSpeed
            };
            entityExtended.Transform.Assign(_transformUtilities.CreateFromModifiedSRTTransform(
                entity.Transform,
                Vector3.One,
                Quaternion.Identity,
                new Vector3(0, 1, 0)));
            var collidableEntities = entities.Where(ground).Where(x => x != entity).ToArray();
            foreach (var collidableEntity in collidableEntities)
            {
                if (this._boundingBoxUtilities.Overlaps(entityExtended, collidableEntity))
                {
                    if (collidableEntity.Transform.LocalPosition.Y > entity.Transform.LocalPosition.Y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The is on ground.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <param name="entities">
        /// The entities.
        /// </param>
        /// <param name="ground">
        /// The ground.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsAtCeiling(IBoundingBox entity, IEnumerable<IBoundingBox> entities, Func<IBoundingBox, bool> ground)
        {
            var entityExtended = new BoundingBox
            {
                Width = entity.Width,
                Height = entity.Height,
                XSpeed = entity.XSpeed,
                YSpeed = entity.YSpeed
            };
            entityExtended.Transform.Assign(_transformUtilities.CreateFromModifiedSRTTransform(
                entity.Transform,
                Vector3.One,
                Quaternion.Identity,
                new Vector3(0, -1, 0)));
            var collidableEntities = entities.Where(ground).Where(x => x != entity).ToArray();
            foreach (var collidableEntity in collidableEntities)
            {
                if (this._boundingBoxUtilities.Overlaps(entityExtended, collidableEntity))
                {
                    if (collidableEntity.Transform.LocalPosition.Y < entity.Transform.LocalPosition.Y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The perform horizontal alignment.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <param name="cellWidth">
        /// The cell width.
        /// </param>
        /// <param name="cellAlignment">
        /// The cell alignment.
        /// </param>
        /// <param name="maxAdjust">
        /// The max adjust.
        /// </param>
        /// <param name="simulateLeft">
        /// The simulate left.
        /// </param>
        /// <param name="simulateRight">
        /// The simulate right.
        /// </param>
        public void PerformHorizontalAlignment(
            IBoundingBox entity, 
            int cellWidth, 
            int cellAlignment, 
            int maxAdjust, 
            Action simulateLeft, 
            Action simulateRight)
        {
            // Perform cell alignment.
            float rem = entity.Transform.LocalPosition.X % cellWidth;
            if (rem > cellWidth - cellAlignment || rem < cellAlignment)
            {
                float targetX = (float)Math.Round(entity.Transform.LocalPosition.X / cellWidth) * cellWidth;
                if (Math.Abs(targetX - entity.Transform.LocalPosition.X) > maxAdjust)
                {
                    if (targetX > entity.Transform.LocalPosition.X)
                    {
                        simulateRight();
                    }
                    else if (targetX < entity.Transform.LocalPosition.X)
                    {
                        simulateLeft();
                    }
                }
                else
                {
                    entity.Transform.LocalPosition += 
                        new Vector3(targetX - entity.Transform.LocalPosition.X, 0, 0);
                }
            }
        }
    }
}
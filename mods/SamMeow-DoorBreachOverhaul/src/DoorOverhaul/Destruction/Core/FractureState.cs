using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DoorOverhaul.Destruction.Core
{
    public enum CellStatus : byte
    {
        Intact,
        Removed,
        Detached
    }

    public enum AnchorKind
    {
        Hinge,
        Latch
    }

    public enum LeafStatus
    {
        Attached,
        Hanging,
        Detached
    }

    public sealed class AnchorSpec
    {
        public AnchorKind Kind;
        public Vector2 Position;
        public float Radius = 0.1f;
    }

    public sealed class Anchor
    {
        public AnchorSpec Spec;
        public float Health;
        public float MaxHealth;
        public int[] Cells;
        public bool Failed;
    }

    public sealed class DetachedGroup
    {
        public int Id;
        public int[] Cells;
        public float Area;
        public Vector2 Centroid;

        public bool IsDebris;

        public bool IsLeafBody;
    }

    public sealed class DestructionResult
    {
        public readonly List<int> RemovedCells = new List<int>();
        public readonly List<int> BrokenBonds = new List<int>();
        public readonly List<DetachedGroup> Groups = new List<DetachedGroup>();
        public readonly List<int> FailedAnchors = new List<int>();
        public bool BecameHanging;
        public int HangingAnchor = -1;
        public bool LeafDetached;

        public bool Changed => RemovedCells.Count > 0 || BrokenBonds.Count > 0 || Groups.Count > 0 || FailedAnchors.Count > 0 || BecameHanging || LeafDetached;
    }

    public sealed class FractureState
    {
        private const int MaxResolvePasses = 6;
        private const float Gravity = 9.81f;

        private readonly float[] _cellHealth;
        private readonly float[] _bondHealth;
        private readonly bool[] _bondBroken;
        private readonly CellStatus[] _status;
        private readonly int[] _groupOf;
        private readonly Anchor[] _anchors;
        private readonly int _initialHinges;
        private int _nextGroupId = 1;

        public FractureLayout Layout { get; }
        public MaterialProfile Profile { get; }
        public float Thickness { get; }
        public IReadOnlyList<Anchor> Anchors => _anchors;
        public LeafStatus Leaf { get; private set; } = LeafStatus.Attached;
        public bool LatchEngaged { get; set; } = true;

        public float MinFragmentArea { get; set; } = 0.01f;

        public float LoadFactor { get; set; } = 1f;

        public bool HingesBreakable { get; set; } = true;

        public FractureState(FractureLayout layout, MaterialProfile profile, float thickness, IEnumerable<AnchorSpec> anchors)
        {
            Layout = layout;
            Profile = profile;
            Thickness = Math.Max(0.005f, thickness);

            _cellHealth = Enumerable.Repeat(profile.CellHealth, layout.Cells.Length).ToArray();
            _bondHealth = Enumerable.Repeat(profile.CellHealth, layout.Bonds.Length).ToArray();
            _bondBroken = new bool[layout.Bonds.Length];
            _status = new CellStatus[layout.Cells.Length];
            _groupOf = new int[layout.Cells.Length];

            _anchors = anchors.Select(spec => new Anchor
            {
                Spec = spec,
                MaxHealth = spec.Kind == AnchorKind.Hinge ? profile.HingeHealth : profile.LatchHealth,
                Health = spec.Kind == AnchorKind.Hinge ? profile.HingeHealth : profile.LatchHealth,
                Cells = CellsWithin(spec.Position, spec.Radius)
            }).ToArray();
            _initialHinges = _anchors.Count(a => a.Spec.Kind == AnchorKind.Hinge);
            foreach (Anchor anchor in _anchors.Where(a => a.Spec.Kind == AnchorKind.Hinge))
            {
                _hingeCells.UnionWith(anchor.Cells);
            }
        }

        public CellStatus StatusOf(int cell) => _status[cell];
        public bool IsIntact(int cell) => _status[cell] == CellStatus.Intact;
        public int GroupOf(int cell) => _groupOf[cell];

        public int LeafBodyGroup { get; private set; } = -1;

        public bool IsPartOfLeaf(int cell) => _status[cell] == CellStatus.Intact || (_status[cell] == CellStatus.Detached && LeafBodyGroup >= 0 && _groupOf[cell] == LeafBodyGroup);

        private bool Damageable(int cell) => IsPartOfLeaf(cell) && (HingesBreakable || !_hingeCells.Contains(cell));

        private readonly HashSet<int> _hingeCells = new HashSet<int>();
        public float CellHealthFraction(int cell) => Math.Max(0f, _cellHealth[cell]) / Profile.CellHealth;
        public float BondHealthFraction(int bond) => _bondBroken[bond] ? 0f : Math.Max(0f, _bondHealth[bond]) / Profile.CellHealth;
        public bool IsBondBroken(int bond) => _bondBroken[bond];
        public int IntactCount => _status.Count(s => s == CellStatus.Intact);

        public void Damage(Vector2 point, ImpactKernel kernel)
        {
            if (kernel.Amount <= 0f)
            {
                return;
            }

            if (kernel.CrackAmount > 0f && kernel.CrackRadius > 0f)
            {
                var cracked = new HashSet<int>();
                foreach (int id in Layout.CellsNear(point, kernel.CrackRadius))
                {
                    foreach (int bond in Layout.Cells[id].Bonds)
                    {
                        if (_bondBroken[bond] || !cracked.Add(bond))
                        {
                            continue;
                        }

                        Bond b = Layout.Bonds[bond];
                        float distance = Poly.DistanceToSegment(point, b.P0, b.P1);
                        if (distance < kernel.CrackRadius)
                        {
                            _bondHealth[bond] -= kernel.CrackAmount * (1f - distance / kernel.CrackRadius);
                        }
                    }
                }
            }

            if (kernel.StruckCellAmount > 0f)
            {
                int struck = Layout.FindCell(point);
                if (struck >= 0 && Damageable(struck))
                {
                    _cellHealth[struck] -= kernel.StruckCellAmount;
                }

                if (kernel.StruckRadius > 0f)
                {
                    foreach (int id in Layout.CellsNear(point, kernel.StruckRadius))
                    {
                        if (id != struck && Damageable(id) && DistanceToCell(point, Layout.Cells[id]) <= kernel.StruckRadius)
                        {
                            _cellHealth[id] -= kernel.StruckCellAmount + kernel.Amount;
                        }
                    }
                }
            }

            var touchedBonds = new HashSet<int>();
            foreach (int id in Layout.CellsNear(point, kernel.Radius))
            {
                if (!Damageable(id))
                {
                    continue;
                }

                Cell cell = Layout.Cells[id];
                float falloff = ImpactModel.Falloff(DistanceToCell(point, cell), kernel.CoreRadius, kernel.Radius);
                if (falloff <= 0f)
                {
                    continue;
                }

                _cellHealth[id] -= kernel.Amount * falloff;
                foreach (int bond in cell.Bonds)
                {
                    touchedBonds.Add(bond);
                }
            }

            foreach (int id in touchedBonds)
            {
                Bond bond = Layout.Bonds[id];
                float falloff = ImpactModel.Falloff(Poly.DistanceToSegment(point, bond.P0, bond.P1), kernel.CoreRadius, kernel.Radius);
                _bondHealth[id] -= kernel.Amount * falloff * Profile.BondDamageFactor;
            }

            foreach (Anchor anchor in _anchors)
            {
                if (anchor.Failed || (!HingesBreakable && anchor.Spec.Kind == AnchorKind.Hinge))
                {
                    continue;
                }

                float distance = Math.Max(0f, Vector2.Distance(point, anchor.Spec.Position) - anchor.Spec.Radius * 0.5f);
                float falloff = ImpactModel.Falloff(distance, kernel.CoreRadius, kernel.Radius + anchor.Spec.Radius);
                anchor.Health -= kernel.Amount * falloff * kernel.AnchorScale;
            }
        }

        public DestructionResult Resolve()
        {
            var result = new DestructionResult();
            if (Leaf == LeafStatus.Detached)
            {
                RemoveDeadCells(result);
                return result;
            }

            for (int pass = 0; pass < MaxResolvePasses; pass++)
            {
                bool changed = RemoveDeadCells(result) | BreakDeadBonds(result) | FailAnchors(result);

                int hinges = _anchors.Count(a => a.Spec.Kind == AnchorKind.Hinge && !a.Failed);
                if (hinges == 0)
                {
                    DetachEverything(result);
                    return result;
                }

                if (Leaf == LeafStatus.Attached && _initialHinges >= 2 && hinges == 1)
                {
                    Leaf = LeafStatus.Hanging;
                    LatchEngaged = false;
                    result.BecameHanging = true;
                    result.HangingAnchor = Array.FindIndex(_anchors, a => a.Spec.Kind == AnchorKind.Hinge && !a.Failed);
                    changed = true;
                }

                int[] depth = SupportDepth();
                if (LoadFactor > 0f && Profile.AllowChunks && BreakOverloadedBonds(depth, result))
                {
                    continue;
                }

                changed |= DetachUnsupported(depth, result);
                if (!changed)
                {
                    return result;
                }
            }

            DetachUnsupported(SupportDepth(), result);
            return result;
        }

        public IEnumerable<(int Cell, Vector2 A, Vector2 B)> ExposedEdges()
        {
            foreach (Bond bond in Layout.Bonds)
            {
                bool a = IsIntact(bond.A);
                bool b = IsIntact(bond.B);
                if (a != b)
                {
                    yield return (a ? bond.A : bond.B, bond.P0, bond.P1);
                }
            }
        }

        private bool RemoveDeadCells(DestructionResult result)
        {
            bool changed = false;
            for (int id = 0; id < _status.Length; id++)
            {
                if (Damageable(id) && _cellHealth[id] <= 0f)
                {
                    _status[id] = CellStatus.Removed;
                    result.RemovedCells.Add(id);
                    changed = true;
                }
            }

            return changed;
        }

        private bool BreakDeadBonds(DestructionResult result)
        {
            bool changed = false;
            for (int id = 0; id < _bondBroken.Length; id++)
            {
                if (!_bondBroken[id] && _bondHealth[id] <= 0f)
                {
                    _bondBroken[id] = true;
                    result.BrokenBonds.Add(id);
                    changed = true;
                }
            }

            return changed;
        }

        private bool FailAnchors(DestructionResult result)
        {
            bool changed = false;
            for (int i = 0; i < _anchors.Length; i++)
            {
                Anchor anchor = _anchors[i];
                if (anchor.Failed)
                {
                    continue;
                }

                if (anchor.Health <= 0f || !anchor.Cells.Any(IsIntact))
                {
                    anchor.Failed = true;
                    result.FailedAnchors.Add(i);
                    if (anchor.Spec.Kind == AnchorKind.Latch)
                    {
                        LatchEngaged = false;
                    }

                    changed = true;
                }
            }

            return changed;
        }

        private bool Connected(int bond)
        {
            Bond b = Layout.Bonds[bond];
            return !_bondBroken[bond] && IsIntact(b.A) && IsIntact(b.B);
        }

        private int[] SupportDepth()
        {
            var depth = Enumerable.Repeat(-1, _status.Length).ToArray();
            var queue = new Queue<int>();
            foreach (Anchor anchor in _anchors)
            {
                if (anchor.Failed || (anchor.Spec.Kind == AnchorKind.Latch && (!LatchEngaged || Leaf != LeafStatus.Attached)))
                {
                    continue;
                }

                foreach (int id in anchor.Cells)
                {
                    if (IsIntact(id) && depth[id] < 0)
                    {
                        depth[id] = 0;
                        queue.Enqueue(id);
                    }
                }
            }

            while (queue.Count > 0)
            {
                int id = queue.Dequeue();
                foreach (int bond in Layout.Cells[id].Bonds)
                {
                    if (!Connected(bond))
                    {
                        continue;
                    }

                    int other = Layout.Bonds[bond].Other(id);
                    if (depth[other] < 0)
                    {
                        depth[other] = depth[id] + 1;
                        queue.Enqueue(other);
                    }
                }
            }

            return depth;
        }

        private bool BreakOverloadedBonds(int[] depth, DestructionResult result)
        {
            var order = Enumerable.Range(0, depth.Length).Where(i => depth[i] > 0).OrderByDescending(i => depth[i]).ToList();
            var load = new float[depth.Length];
            float weightPerArea = Thickness * Profile.Density * Gravity * LoadFactor;
            var parents = new List<int>(8);
            bool broke = false;

            float minEffectiveLength = 0.3f * Math.Min(Layout.CellWidth, Layout.CellHeight);

            foreach (int id in order)
            {
                load[id] += Layout.Cells[id].Area * weightPerArea;

                parents.Clear();
                float totalLength = 0f;
                float capacity = 0f;
                foreach (int bond in Layout.Cells[id].Bonds)
                {
                    if (Connected(bond) && depth[Layout.Bonds[bond].Other(id)] == depth[id] - 1)
                    {
                        parents.Add(bond);
                        totalLength += Layout.Bonds[bond].Length;
                        float health = BondHealthFraction(bond);
                        capacity += Profile.BondStrength * Math.Max(Layout.Bonds[bond].Length, minEffectiveLength) * health * health;
                    }
                }

                if (parents.Count == 0)
                {
                    continue;
                }

                if (load[id] > capacity)
                {
                    foreach (int bond in parents)
                    {
                        _bondBroken[bond] = true;
                        _bondHealth[bond] = 0f;
                        result.BrokenBonds.Add(bond);
                    }

                    broke = true;
                    continue;
                }

                foreach (int bond in parents)
                {
                    load[Layout.Bonds[bond].Other(id)] += load[id] * Layout.Bonds[bond].Length / totalLength;
                }
            }

            return broke;
        }

        private bool DetachUnsupported(int[] depth, DestructionResult result)
        {
            bool changed = false;
            var visited = new bool[_status.Length];
            for (int id = 0; id < _status.Length; id++)
            {
                if (!IsIntact(id) || depth[id] >= 0 || visited[id])
                {
                    continue;
                }

                List<int> component = Flood(id, visited);
                DetachedGroup group = MakeGroup(component, isLeafBody: false);
                if (!Profile.AllowChunks)
                {
                    group.IsDebris = true;
                }

                Commit(group, result);
                changed = true;
            }

            return changed;
        }

        private void DetachEverything(DestructionResult result)
        {
            var visited = new bool[_status.Length];
            var groups = new List<DetachedGroup>();
            for (int id = 0; id < _status.Length; id++)
            {
                if (IsIntact(id) && !visited[id])
                {
                    groups.Add(MakeGroup(Flood(id, visited), isLeafBody: false));
                }
            }

            if (groups.Count > 0)
            {
                DetachedGroup body = groups.OrderByDescending(g => g.Area).First();
                body.IsLeafBody = true;
                body.IsDebris = false;
            }

            foreach (DetachedGroup group in groups)
            {
                Commit(group, result);
            }

            DetachedGroup leafBody = groups.Find(g => g.IsLeafBody);
            LeafBodyGroup = leafBody != null ? leafBody.Id : -1;
            Leaf = LeafStatus.Detached;
            LatchEngaged = false;
            result.LeafDetached = true;
        }

        private List<int> Flood(int start, bool[] visited)
        {
            var component = new List<int>();
            var stack = new Stack<int>();
            stack.Push(start);
            visited[start] = true;
            while (stack.Count > 0)
            {
                int id = stack.Pop();
                component.Add(id);
                foreach (int bond in Layout.Cells[id].Bonds)
                {
                    if (!Connected(bond))
                    {
                        continue;
                    }

                    int other = Layout.Bonds[bond].Other(id);
                    if (!visited[other])
                    {
                        visited[other] = true;
                        stack.Push(other);
                    }
                }
            }

            component.Sort();
            return component;
        }

        private DetachedGroup MakeGroup(List<int> cells, bool isLeafBody)
        {
            float area = 0f;
            Vector2 weighted = Vector2.Zero;
            foreach (int id in cells)
            {
                area += Layout.Cells[id].Area;
                weighted += Layout.Cells[id].Centroid * Layout.Cells[id].Area;
            }

            return new DetachedGroup
            {
                Cells = cells.ToArray(),
                Area = area,
                Centroid = area > 0f ? weighted / area : Vector2.Zero,
                IsDebris = area < MinFragmentArea,
                IsLeafBody = isLeafBody
            };
        }

        private void Commit(DetachedGroup group, DestructionResult result)
        {
            group.Id = _nextGroupId++;
            foreach (int id in group.Cells)
            {
                _status[id] = CellStatus.Detached;
                _groupOf[id] = group.Id;
            }

            result.Groups.Add(group);
        }

        private int[] CellsWithin(Vector2 position, float radius)
        {
            var cells = new List<int>();
            foreach (int id in Layout.CellsNear(position, radius))
            {
                if (DistanceToCell(position, Layout.Cells[id]) <= radius)
                {
                    cells.Add(id);
                }
            }

            if (cells.Count == 0)
            {
                cells.Add(Layout.FindCell(position));
            }

            return cells.ToArray();
        }

        private static float DistanceToCell(Vector2 point, Cell cell)
        {
            if (Poly.ContainsConvex(cell.Polygon, point))
            {
                return 0f;
            }

            float best = float.MaxValue;
            for (int i = 0; i < cell.Polygon.Length; i++)
            {
                best = Math.Min(best, Poly.DistanceToSegment(point, cell.Polygon[i], cell.Polygon[(i + 1) % cell.Polygon.Length]));
            }

            return best;
        }
    }
}

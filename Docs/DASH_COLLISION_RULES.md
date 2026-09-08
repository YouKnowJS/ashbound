# Dash collision rules

Player Dash uses two explicit world-collision categories:

| Layer | Index | Normal walking | Player Dash |
|---|---:|---|---|
| `InternalObstacle` | 9 | Collides | Temporarily ignored per player controller |
| `WorldBoundary` | 10 | Collides | Always collides |

`RoomView` assigns authored combat obstacles and colliders from the optional environment prefab to `InternalObstacle`. Polygon walls, optional boundary prefabs, and legacy arena walls use `WorldBoundary`. Actor-to-actor collision remains disabled on the existing `Actor` layer 8.

## Dash resolution

`ActorMotor.TryDash` asks the active `RoomView` to resolve the complete requested path before movement begins. `RoomView.ResolveDashEndpoint` samples the path against both:

1. the union of authored playable sections, including connected transition sections;
2. the authored irregular boundary polygon with player-radius clearance.

The first invalid sample stops the path, so a dash cannot bridge an unauthored gap or void even when its requested endpoint lies in another valid island. The chosen endpoint is then checked against both collision categories to avoid restoring collision while embedded in geometry.

Only the dashing player's `CharacterController` ignores the active room's internal colliders. The override is restored when the dash reaches its endpoint, expires, is stopped, movement becomes unavailable, or the component is disabled. Normal movement and other actors keep their usual collisions. World-boundary collision is never ignored and provides a physical backstop in addition to endpoint resolution.

## Debug visualization

Open **F1 → Camera → Dash collision overlay**:

- orange: `InternalObstacle` colliders;
- cyan: `WorldBoundary` colliders;
- yellow: requested dash path;
- green: resolved legal dash path;
- magenta marker / `RESOLVED`: final endpoint.

The overlay is diagnostic only. Enabling it marks active telemetry as debug-modified.

## Automated coverage

Play Mode tests use a deliberately irregular two-room arena with a narrow authored connector and a void gap. They verify wall and pillar traversal, restored collision, walking obstruction, world-edge stopping, irregular-polygon containment, void rejection, and traversal across connected subspaces. Edit Mode verifies the three dedicated layer assignments and masks.

## Limitations

The playable union is built from rectangular `CombatSpaceSection` data, including rotated sections; it is not a navigation mesh. Runtime environment prefabs are treated as internal obstacles unless placed under the explicit boundary-prefab hook. Future authored cliff volumes should use `WorldBoundary` rather than the environment-prefab hook.

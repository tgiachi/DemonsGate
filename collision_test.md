# Analisi Sistema di Collisioni SquidCraft

## Come Funziona Attualmente

### 1. Setup della Collisione (Game1.cs:162)
```csharp
_cameraComponent.CheckCollision = (pos, size) => _worldComponent.IsBlockSolid(pos);
```

### 2. Fisica della Camera (CameraComponent.cs:277-290)
```csharp
private void ApplyPhysics(float deltaTime)
{
    _velocity.Y -= Gravity * deltaTime;  // Applica gravità
    var newPosition = _position + _velocity * deltaTime;
    
    if (CheckCollision != null)
    {
        newPosition = ResolveCollisions(newPosition);  // Risolve collisioni
    }
    
    _position = newPosition;
}
```

### 3. Risoluzione Collisioni (CameraComponent.cs:292-329)
```csharp
private Vector3 ResolveCollisions(Vector3 newPosition)
{
    var halfSize = BoundingBoxSize * 0.5f;  // Bounding box giocatore (0.6×1.8×0.6)
    _isOnGround = false;
    
    // SOLO collisioni verticali (Y) quando si cade
    if (_velocity.Y <= 0)
    {
        var feetY = newPosition.Y - BoundingBoxSize.Y * 0.5f;
        var blockY = (int)MathF.Floor(feetY);
        
        // Controlla area sotto i piedi del giocatore
        for (float x = minX; x <= maxX; x += 0.3f)
        {
            for (float z = minZ; z <= maxZ; z += 0.3f)
            {
                if (CheckCollision(testPos, Vector3.Zero) == true)
                {
                    // Posiziona giocatore sopra il blocco
                    resolved.Y = blockY + 1.0f + BoundingBoxSize.Y * 0.5f;
                    _velocity.Y = 0;
                    _isOnGround = true;
                }
            }
        }
    }
    
    return resolved;  // NO collisioni orizzontali!
}
```

### 4. Controllo Blocco Solido (WorldComponent.cs:106-133)
```csharp
public bool IsBlockSolid(XnaVector3 worldPosition)
{
    // Converte posizione mondo → chunk locale
    var blockX = (int)MathF.Floor(worldPosition.X);
    var blockY = (int)MathF.Floor(worldPosition.Y);
    var blockZ = (int)MathF.Floor(worldPosition.Z);
    
    // Trova chunk
    var chunkEntity = GetChunkEntity(/*...*/);
    if (chunkEntity == null) return false;
    
    // Controlla se il blocco esiste e non è Air
    var block = chunkEntity.GetBlock(localX, localY, localZ);
    return block != null && block.BlockType != BlockType.Air;
}
```

## PROBLEMI IDENTIFICATI

### ❌ 1. Mancano Collisioni Orizzontali
Il sistema gestisce SOLO collisioni verticali (Y). Il giocatore può attraversare i muri!

### ❌ 2. Movimento Fisica vs Fly Mode Inconsistente
- **Fisica Mode**: Solo movimento orizzontale (WASD), gravità, salto
- **Fly Mode**: Movimento 3D completo
- **Bug**: Entrambi usano la stessa funzione `ResolveCollisions()` che non gestisce X/Z

### ❌ 3. Controllo Collisioni Troppo Semplice
- Controlla solo un singolo punto per blocco
- Non considera la forma del bounding box del giocatore
- Campionamento sparso (ogni 0.3 unità)

### ❌ 4. Bounding Box del Giocatore
- Default: 0.6×1.8×0.6 (ragionevole)
- Ma non usato per collisioni orizzontali

## COSA DOVREBBE FARE

### ✅ Collisioni Complete
1. **Verticali (Y)**: Prevenire caduta attraverso blocchi ✓
2. **Orizzontali (X/Z)**: Prevenire attraversamento muri ❌
3. **Salto**: Solo quando `IsOnGround = true` ✓

### ✅ Test Proposti
1. **Camminare contro un muro** → Dovrebbe fermarsi
2. **Saltare contro un soffitto** → Dovrebbe fermarsi
3. **Cadere su un blocco** → Dovrebbe atterrare sopra ✓
4. **Camminare su un bordo** → Dovrebbe cadere con gravità ✓

## RACCOMANDAZIONI

### 1. Aggiungere Collisioni Orizzontali
```csharp
private Vector3 ResolveCollisions(Vector3 newPosition)
{
    // 1. Risolvi collisioni X
    newPosition = ResolveAxisCollision(newPosition, Vector3.UnitX);
    
    // 2. Risolvi collisioni Z  
    newPosition = ResolveAxisCollision(newPosition, Vector3.UnitZ);
    
    // 3. Risolvi collisioni Y (esistente)
    newPosition = ResolveVerticalCollisions(newPosition);
    
    return newPosition;
}
```

### 2. Migliorare Controllo Collisioni
- Controllare tutti gli 8 angoli del bounding box
- Usare sweep testing per movimento continuo
- Separare logica per X, Y, Z

### 3. Debug Visivo
- Mostrare bounding box del giocatore
- Evidenziare blocchi in collisione
- Log delle collisioni rilevate

Vuoi che implementi le correzioni per il sistema di collisioni?
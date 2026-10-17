ГАЙД: СОЗДАНИЕ УНИКАЛЬНЫХ ПАТРОНОВ И МАГАЗИНОВ SOTI-REBORN(CS_SS14)

Цель: патроны от одного оружия не лезут в другое. Для каждого ствола — свой тег патрона (Cart_XXX) и магазина (Mag_XXX).

1. ТЕГ ПАТРОНА
Файл: cartridges_tags.yml
Добавь:
- type: Tag
  id: Cart_XXX

2. ТЕГ МАГАЗИНА
Файл: magazines_tags.yml
Добавь:
- type: Tag
  id: Mag_XXX

3. ПАТРОН
Файл: cs_cartridges.yml
- type: entity
  parent: [BaseCartridge, BaseSecurityContraband]
  id: CartridgeCSXXX
  name: cartridge (xxx)
  components:
  - type: Tag
    tags: [ Cart_XXX ]
  - type: CartridgeAmmo
    proto: BulletCSXXX
  - type: Sprite
    sprite: Objects/Weapons/Guns/Ammunition/Casings/ammo_casing.rsi
    layers: [ { state: base, map: ["enum.AmmoVisualLayers.Base"] } ]
  - type: Appearance
  - type: SpentAmmoVisuals

4. МАГАЗИН
Файл: cs_magazines.yml
- type: entity
  parent: BaseMagazinePistol   # или Magnum, LightRifle, Rifle, SMG
  id: MagazineCSXXX
  name: magazine (xxx)
  components:
  - type: Tag
    tags: [ Mag_XXX ]
  - type: BallisticAmmoProvider
    whitelist: { tags: [ Cart_XXX ] }
    capacity: 15
    proto: CartridgeCSXXX
    soundInsert: { path: /Audio/Weapons/Guns/MagIn/pistol_magin.ogg }

5. ПУЛЯ (HITSCAN)
Файл: cs_hitscan.yml
- type: entity
  id: BulletCSXXX
  parent: BasicCSHitscan
  name: cs xxx bullet
  categories: [ HideSpawnMenu ]
  components:
  - type: HitscanBasicDamage
    damage:
      types:
        Piercing: 25
  - type: HitscanBasicVisuals
    bullet:
      sprite:
        sprite: Objects/Weapons/Guns/Projectiles/projectiles2.rsi
        state: bullet

6. ПОДКЛЮЧЕНИЕ К ОРУЖИЮ
В прототипе оружия:
- type: MagazineAmmoProvider
  whitelist: { tags: [ Mag_XXX ] }
  proto: MagazineCSXXX
Либо для дробовиков/револьверов:
- type: BallisticAmmoProvider
  whitelist: { tags: [ Cart_XXX ] }
  capacity: 6
  proto: CartridgeCSXXX

ЧЕКЛИСТ (почему не работает):
- Тег не создан или опечатка в ID (регистр важен).
- В магазине whitelist не совпадает с тегом патрона.
- В оружии остался старый whitelist (MagazinePistol и т.п.).
- CartridgeAmmo.proto ссылается на несуществующую пулю.
- Дубль ID сущности.

СХЕМА ИМЁН:
Тег патрона: Cart_XXX
Тег магазина: Mag_XXX
Патрон: CartridgeCSXXX
Магазин: MagazineCSXXX
Пуля: BulletCSXXX
Дробь: PelletCSXXXSpread
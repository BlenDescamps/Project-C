# 🗺️ ROADMAP DE PRODUCTION IDÉALE — WILD HOLE: TAVERN & CLEAVER
**Équipe :** 2 Développeurs + 1 Artiste 3D  
**Format :** Solo  
**Durée Cible Plein Temps :** **9 Mois** *(Équivalent ~14-16 mois en temps partiel)*  
**Stratégie Clé :** Réutilisation d'assets modulaires existants + Zéro Netcode multijoueur.

---

## 👥 RÉPARTITION DES RÔLES
* **Dev 1 (Gameplay, Combat & Physique) :** Contrôleur FPS, maniement des armes, physique des ustensiles en cuisine, mécaniques de boss et puzzles environnementaux.
* **Dev 2 (Systèmes, IA, UI & Économie) :** IA des monstres & file d'attente clients, système de QTE de Kuko, gestion de l'inventaire/recettes, calendrier des 30 jours, boutique, audio et intégration Steam.
* **Artiste (Hero Assets, Level Art & Anim) :** Adaptation et mise en place des décors (avec assets existants), modélisation/rig/anim des 4 Boss + Dragon, design expressif de Kuko, modélisation des viandes/ingrédients et VFX cartoon.

---

## 📅 CALENDRIER PAR PHASES & JALONS (MILESTONES)

```
[Mois 1 - 2]  ──▶  PHASE 1 : VERTICAL SLICE & PROTOTYPE JOUABLE
[Mois 3 - 4]  ──▶  PHASE 2 : LA TAVERNE COMPLÈTE & BIOMES 1 ET 2
[Mois 5 - 6]  ──▶  PHASE 3 : BIOMES 3 ET 4 & COMBAT FINAL
[Mois 7]      ──▶  PHASE 4 : ALPHA & CONTENU ALTERNATIF (DRAGON)
[Mois 8]      ──▶  PHASE 5 : BETA, JUICE & ÉQUILIBRAGE
[Mois 9]      ──▶  PHASE 6 : POLISH, STEAMWORKS & RELEASE
```

---

### 🟢 PHASE 1 : VERTICAL SLICE & CORE LOOP (Mois 1 – Mois 2)
> **Objectif :** Valider manette en main la boucle élémentaire : *1 arme ➔ 1 monstre ➔ découpe Kuko ➔ cuisson physique ➔ 1 client servi.*

* **Dev 1 :** 
  * Contrôleur FPS fluide (déplacements, dash, lancer d'objets).
  * Système physique de cuisine (poser une poêle, cuire un steak, jauge de cuisson simple).
* **Dev 2 :** 
  * Système de QTE de boucherie avec Kuko.
  * Première IA de monstre (comportement d'attaque basique).
  * 1 client d'essai avec jauge de patience.
* **Artiste :** 
  * Modélisation et rig de Kuko (lame + yeux/bouche expressifs).
  * Intégration d'une arène de test avec les assets modulaires existants.
  * 1 modèle de monstre (Bovidé) + 2 viandes découpées.
* 🎯 **Gate Check Fin M2 :** Le prototype est-il jouable et immédiatement amusant ?

---

### 🟡 PHASE 2 : FONDATIONS SYSTÉMIQUES & BIOMES 1 & 2 (Mois 3 – Mois 4)
> **Objectif :** Rendre la taverne 100 % opérationnelle avec sa file d'attente et intégrer les 2 premiers biomes complets.

* **Dev 1 :** 
  * Lancer d'objets et système de plat qui tombe par terre (contamination).
  * Boss 1 : Le Minotaure (charges, destructions d'arène).
  * Boss 2 : Le Kraken (attaques de tentacules, esquives).
* **Dev 2 :** 
  * Système complet de la file d'attente Food-truck (génération de clients, tolérance, réputation).
  * Intégration du risque d'intoxication alimentaire et du plat de secours.
  * Salle des portails sous la taverne (sélection irréversible).
* **Artiste :** 
  * Level Art du Biome 1 (Terre / Labyrinthe) et Biome 2 (Eau / Abysses) à base d'assets réutilisés.
  * Modélisation & animation du Minotaure et du Kraken.
  * 5 modèles de plats cuisinés et ingrédients marins/bovins.
* 🎯 **Gate Check Fin M4 :** On peut enchaîner 5 journées de jeu complètes avec les deux premiers boss.

---

### 🟠 PHASE 3 : EXPANSION DU CONTENU & CLIMAX (Mois 5 – Mois 6)
> **Objectif :** Intégrer les Biomes 3 et 4, les puzzles environnementaux et la narration du patron possédé.

* **Dev 1 :** 
  * Boss 3 : Le Poulet Géant (attaques de zone aériennes).
  * Boss 4 : Le Chef possédé (Phase 1) & Démon de la Sœur (Phase 2).
  * Mécaniques de puzzles environnementaux dans l'arène de lave (leviers, pièges pour étourdir le boss).
* **Dev 2 :** 
  * Calendrier complet des 30 jours avec l'horloge et sauvegarde automatique.
  * Boutique de nuit : amélioration d'armes, achat d'armures et recettes.
  * Système de dialogue dynamique de Kuko (réactions vocales/textuelles).
* **Artiste :** 
  * Level Art du Biome 3 (Air) et Biome 4 (Feu / Enfers).
  * Modélisation du Poulet Géant, du Patron possédé et de la Grande Sœur démoniaque.
  * VFX cartoon (feu, fumée de cuisson, éclaboussures de sauce, étoiles de stun).
* 🎯 **Gate Check Fin M6 :** L'histoire principale est jouable du début à la fin sans interruption.

---

### 🔴 PHASE 4 : ALPHA COMPLETE & CONTENU ALTERNATIF (Mois 7)
> **Objectif :** Verrouiller toutes les fonctionnalités (Feature Freeze) et intégrer l'embranchement du Dragon.

* **Dev 1 :** 
  * Implémentation du combat de l'Usurier : Le Dragon des Abysses (Portail d'urgence si dette impayée au Jour 30).
  * Polissage des collisions physiques en cuisine.
* **Dev 2 :** 
  * Service Gastronomique VIP (déclenché après chaque boss abattu).
  * Cinématiques de fin : Banquet final, dialogue du patron, adoubement de Kuko.
* **Artiste :** 
  * Modélisation du Dragon des Abysses et du modèle de Kuko évolué en "Vrai Chef".
  * UI finale (tableau de bord des recettes, HUD propre, ardoise du jour).
* 🎯 **Gate Check Fin M7 :** Le jeu est **Alpha** : on peut gagner ou perdre la dette, et terminer l'aventure.

---

### 🟣 PHASE 5 : BETA, JUICE & ÉQUILIBRAGE (Mois 8)
> **Objectif :** Transformer un jeu fonctionnel en un jeu croustillant et ultra-satisfaisant.

* **Dev 1 & Dev 2 :** 
  * Équilibrage minutieux : prix des plats, montant de la dette, dégâts des armes.
  * "Game Feel" : Screenshakes, arrêts sur image (hitstop), sensation d'impact des coups de couteau.
  * Intégration audio complète : bruits de friture réalistes, tranchant des lames, musiques de rush.
* **Artiste :** 
  * Éclairage (Lighting) et post-processing pour sublimer les scènes.
  * Polish des animations et visages de Kuko selon ses humeurs.
* 🎯 **Gate Check Fin M8 :** Version Beta prête pour les playtests externes et la démo Steam.

---

### ⚪ PHASE 6 : QA, STEAM & LANCEMENT (Mois 9)
> **Objectif :** Zéro bug bloquant, intégration magasin et publication.

* **Toute l'équipe :**
  * Correction massive des bugs issus des playtests.
  * Intégration Steamworks (Succès Steam, Cloud Saves, compatibilité Steam Deck).
  * Optimisation des performances (60 FPS constant).
  * Préparation des assets de la page Steam (capsules, bande-annonce, gifs de gameplay).
* 🚀 **Sortie :** Lancement de l'Early Access ou de la version 1.0 !

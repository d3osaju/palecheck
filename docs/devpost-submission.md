# Devpost submission: PaleCheck

**Track:** Coding · **Category:** Biology/Medical and Environmental Science
**Live app:** https://palecheck.zetalabs.in · **One-tap demo for judges:** https://palecheck.zetalabs.in/check?sample=3
**Code:** https://github.com/d3osaju/palecheck (make it public before submitting)

**Tagline (≤ 200 chars):**
I tried to build an AI that spots anaemia in an eyelid photo — and found the data and the light bulb were the real problem. PaleCheck fixes the light with a ₹1 card.

**Built with:** C#, .NET 10, Blazor WebAssembly, PWA, Python, NumPy, Pillow, xUnit, HTML Canvas, GitHub Actions, Vercel

**Gallery images (in this order):** `docs/gallery/poster-hook.png`, `poster-audit.png`, `poster-lighting.png`,
`poster-honest.png`, `phone-result.png`, `phone-home.png`, `phone-science.png`, `phone-dupescope.png`,
then your real Lighting Lab screenshot.

---

## Inspiration

Almost 6 in 10 Indian girls aged 15–19 are anaemic (NFHS-5: 59.1%). Anaemia makes you tired, breathless and
slow to concentrate, and most people never find out because finding out needs a blood test.

Nurses have a trick: pull down the lower eyelid and look at how pale it is. AI apps promise to do the same with a
phone, and the largest public dataset for this reports up to 84.8% accuracy. I wanted to build that for teenagers
in Kerala. Before trusting any number, I decided to check the data myself.

## What it does

PaleCheck turns a phone into a colour-measuring tool for the inner eyelid:

1. **Print the ₹1 card** (or use plain white paper). Its QR code opens the app.
2. **Take a photo** with the card beside your eye. PaleCheck **finds the card by itself**; you **tap your
   eyelid once** and Smart select outlines it.
3. It **removes the colour of the room light** using the card, rejects glare and shadow, and measures the
   eyelid's colour. Take up to 5 photos and it **averages** them for a steadier reading.
4. You get a **pallor index**, *where you sit among 378 reference eyes* (with an honest "overlap zone" where
   healthy and anaemic eyes look the same), a **symptom checklist** that says "get tested" whatever the photo
   shows, a referral to the **free Anaemia Mukt Bharat blood test**, and iron-rich local foods.

It works **offline**, in **English, Malayalam and Hindi**, and **no photo ever leaves the phone**.

Two extra tools: the **Lighting Lab** lets anyone measure the card's effect themselves, and **DupeScope**
checks any image dataset for the problem I found, right in the browser.

## How I built it

- **Data audit:** I hashed the pixels of all 710 images in CP-AnemiC. **312 images** are identical photos
  filed under *different children* (different hospital, age, sex and haemoglobin). One photo appears as 11
  children with Hb from 4.6 to 10.9 g/dL. Under the usual random 5-fold cross-validation, a "model" that only
  memorises photos scores **70.7%**.
- **DupeScope found a second layer, in my own pipeline:** I turned the audit into a reusable tool. Its
  near-duplicate test found **12 more images** that are re-cropped copies of another record's photo, and 3 of
  those pairs sat on *both sides of my own train/test split*. I fixed the rule, re-ran everything (protocol v2)
  and report both runs. In total **324 of 710 images (45.6%)** are copies.
- **Honest validation:** on the 386 trustworthy images, the protocol is fixed in advance: 8 demo images held
  out, a 30% **lockbox** opened once, every choice made by nested cross-validation on the rest, then
  leave-one-hospital-out, subgroup and confound checks, with bootstrap confidence intervals.
- **Model:** L2-regularised logistic regression written from scratch in NumPy on CIELAB colour features.
  The whole model is 10 numbers and runs instantly in the browser.
- **Lighting physics:** I re-lit every reference eyelid with blackbody lights (2700–5000 K) and measured how far
  redness moves, then built the card correction (von Kries white balance in linear RGB).
- **App (C# / Blazor WebAssembly PWA):** automatic card detection (a bright patch inside a dark frame), Smart
  select (region growing in CIELAB, where lightness counts less than hue because the eyelid is curved),
  quality gates, SVG charts, local-only history.
- **Engineering:**
  - 28 xUnit tests prove the C# app reproduces Python's features to 10⁻⁹.
  - **CI re-downloads the public dataset and re-derives every headline number** on each push.
  - Strict Content-Security-Policy.
  - 2.2 MB first download.

## Challenges I ran into

- **The accuracy collapsed.** With copies removed, colour alone reaches AUC 0.65 (nested CV 0.65; lockbox 0.65,
  95% CI 0.55–0.75), far below the published 0.835. I built the project around that honesty instead of hiding it.
- **Lighting is a bigger signal than anaemia.** Healthy and anaemic eyelids differ by 3.7 units of redness; a
  yellow bulb moves it by 15.5, **4.2× the disease**.
- **My own leak.** Finding near-copies straddling my own split was uncomfortable, and fixing it in public was the right call.
- Making C# and Python agree exactly, and writing health advice in three languages without over-claiming.

## Accomplishments that I'm proud of

- Finding, documenting and publishing a data-quality problem in a public medical-AI benchmark, with cleaned
  manifests other researchers can use.
- DupeScope: a tool that catches this in any dataset, and caught it in mine.
- In a synthetic test, the same eyelid under a warm bulb and in daylight gets the **same** pallor index (25 and
  25) after card correction. With my real phone: **[fill in from docs/real-camera-experiment.md]**.
- A real, offline, private, multilingual app with a one-tap demo.

## What I learned

- Data quality matters more than model choice. A memorising model can look 70% accurate.
- Honest validation: lockboxes, nested cross-validation, confidence intervals, and reporting a result that got worse.
- Colour science: gamma, CIELAB, white balance, and why a phone camera is not a colour meter.
- In health tools, "I can't tell, get a blood test" is a feature.

## What's next for PaleCheck

1. Real-camera Lighting Lab results across several phones and lights.
2. A calibrated dataset of Indian teenagers (consented, recent haemoglobin, card in every photo, numbers only),
   then retrain and re-validate for the 12 g/dL threshold.
3. Personal tracking during the weekly blue iron-tablet programme.
4. Share the audit with the dataset authors so the benchmark can be corrected.

---

## AI use (disclose this)

The rules say AI use for writing code/research documents is *discouraged but allowed*, and that product quality is
what is judged. Say clearly which parts you did and which an AI assistant helped with, and make sure you can explain
every step yourself. The judges include AI researchers.

## Judging criteria map (for you, not for pasting)

| Criterion (25% each) | Where PaleCheck earns it |
|---|---|
| Innovation & Creativity | Audit plot twist; DupeScope catching its own leak; ₹1 card with auto-detection; overlap-zone result design |
| Impact & Relevance (tiebreaker) | 59.1% prevalence; symptom checklist + free test referral; offline, private, Malayalam/Hindi; public cleaned data |
| Execution & Technical Quality | Lockbox, nested CV, LOHO, CIs; v1→v2 transparency; parity tests; CI re-derives numbers; CSP; 2.2 MB |
| Presentation & Communication | Two-twist story, one-tap demo link, posters, airplane-mode demo; see `video-script.md` |

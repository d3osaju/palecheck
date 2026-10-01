namespace PaleCheck.Web.Services;

/// <summary>
/// English / Malayalam / Hindi strings for everything a person doing a check reads.
/// The research pages (Science, Lighting Lab) stay in English for judges and reviewers.
/// </summary>
public sealed class Lang(Js js)
{
    public static readonly (string Code, string Name)[] Languages = [("en", "English"), ("ml", "മലയാളം"), ("hi", "हिन्दी")];
    private const string Key = "palecheck.lang";

    public string Current { get; private set; } = "en";
    public event Action? Changed;

    public async Task InitAsync()
    {
        var saved = await js.InvokeAsync<string?>("storageGet", Key);
        if (saved is "en" or "ml" or "hi") Current = saved;
        await js.InvokeVoidAsync("setLang", Current);
        Changed?.Invoke();
    }

    public async Task SetAsync(string code)
    {
        Current = code;
        await js.InvokeAsync<bool>("storageSet", Key, code);
        await js.InvokeVoidAsync("setLang", code);
        Changed?.Invoke();
    }

    public string this[string key] => T.TryGetValue(key, out var s)
        ? Current switch { "ml" => s.Ml, "hi" => s.Hi, _ => s.En }
        : key;

    public string F(string key, params object[] args) => string.Format(this[key], args);

    private static readonly Dictionary<string, (string En, string Ml, string Hi)> T = new()
    {
        ["nav.home"] = ("Home", "ഹോം", "होम"),
        ["nav.check"] = ("Check", "പരിശോധന", "जाँच"),
        ["nav.lab"] = ("Lighting Lab", "ലൈറ്റ് ലാബ്", "लाइट लैब"),
        ["nav.science"] = ("Science", "ശാസ്ത്രം", "विज्ञान"),
        ["nav.card"] = ("Card", "കാർഡ്", "कार्ड"),
        ["nav.history"] = ("History", "ചരിത്രം", "इतिहास"),

        ["home.kicker"] = ("Anaemia screening, honestly", "വിളർച്ച പരിശോധന, സത്യസന്ധമായി", "एनीमिया जाँच, ईमानदारी से"),
        ["home.title"] = ("Your eyelid. A ₹1 card. The truth about phone anaemia checks.",
                          "നിങ്ങളുടെ കൺപോള. ഒരു ₹1 കാർഡ്. ഫോൺ വഴിയുള്ള വിളർച്ച പരിശോധനയുടെ സത്യം.",
                          "आपकी पलक। ₹1 का कार्ड। फ़ोन से एनीमिया जाँच का सच।"),
        ["home.lede"] = ("Doctors look for anaemia by checking how pale the inside of your lower eyelid is. PaleCheck measures that colour with your phone — and corrects for room lighting with a printed white card.",
                         "താഴത്തെ കൺപോളയുടെ ഉൾഭാഗം എത്ര വിളറിയിരിക്കുന്നു എന്ന് നോക്കിയാണ് ഡോക്ടർമാർ വിളർച്ച തിരിച്ചറിയുന്നത്. PaleCheck ആ നിറം നിങ്ങളുടെ ഫോൺ ഉപയോഗിച്ച് അളക്കുന്നു — ഒരു വെള്ള കാർഡ് ഉപയോഗിച്ച് മുറിയിലെ വെളിച്ചത്തിന്റെ സ്വാധീനം തിരുത്തുന്നു.",
                         "डॉक्टर निचली पलक के अंदरूनी हिस्से का पीलापन देखकर एनीमिया पहचानते हैं। PaleCheck आपके फ़ोन से वह रंग मापता है — और एक छपे हुए सफ़ेद कार्ड से कमरे की रोशनी का असर ठीक करता है।"),
        ["home.start"] = ("Start a check", "പരിശോധന തുടങ്ങുക", "जाँच शुरू करें"),
        ["home.science"] = ("See the science", "ശാസ്ത്രം കാണുക", "विज्ञान देखें"),
        ["home.stat1"] = ("of Indian girls aged 15–19 are anaemic (NFHS-5)", "ഇന്ത്യയിലെ 15–19 വയസ്സുള്ള പെൺകുട്ടികൾക്ക് വിളർച്ചയുണ്ട് (NFHS-5)", "भारत में 15–19 वर्ष की लड़कियाँ एनीमिया से ग्रस्त हैं (NFHS-5)"),
        ["home.stat2"] = ("of the largest public eyelid dataset is reused photos", "ഏറ്റവും വലിയ പൊതു കൺപോള ഡാറ്റാസെറ്റിന്റെ ഭാഗം ആവർത്തിച്ച ഫോട്ടോകളാണ്", "सबसे बड़े सार्वजनिक पलक डेटासेट का हिस्सा दोहराई गई फ़ोटो हैं"),
        ["home.stat3"] = ("a yellow bulb shifts eyelid redness more than anaemia does", "ഒരു മഞ്ഞ ബൾബ് വിളർച്ചയേക്കാൾ കൂടുതൽ കൺപോളയുടെ ചുവപ്പ് മാറ്റുന്നു", "पीला बल्ब पलक की लाली को एनीमिया से ज़्यादा बदलता है"),
        ["home.how"] = ("How a check works", "പരിശോധന എങ്ങനെ", "जाँच कैसे होती है"),
        ["home.how1"] = ("Print the white card (or use plain white paper).", "വെള്ള കാർഡ് പ്രിന്റ് ചെയ്യുക (അല്ലെങ്കിൽ വെള്ള പേപ്പർ ഉപയോഗിക്കുക).", "सफ़ेद कार्ड छापें (या सादा सफ़ेद कागज़ इस्तेमाल करें)।"),
        ["home.how2"] = ("Photograph your inner eyelid with the card beside it.", "കാർഡ് അടുത്ത് പിടിച്ച് ഉൾക്കൺപോളയുടെ ഫോട്ടോ എടുക്കുക.", "कार्ड को पास रखकर पलक के अंदरूनी हिस्से की फ़ोटो लें।"),
        ["home.how3"] = ("PaleCheck finds the card itself. Tap your eyelid once and read your result.",
                         "PaleCheck കാർഡ് സ്വയം കണ്ടെത്തും. കൺപോളയിൽ ഒരിക്കൽ തൊടുക, ഫലം വായിക്കുക.",
                         "PaleCheck कार्ड खुद ढूँढ लेता है। पलक पर एक बार टैप करें और परिणाम पढ़ें।"),
        ["home.private"] = ("Everything runs on your phone. No photo ever leaves it.", "എല്ലാം നിങ്ങളുടെ ഫോണിൽ തന്നെ. ഒരു ഫോട്ടോയും പുറത്തേക്ക് പോകുന്നില്ല.", "सब कुछ आपके फ़ोन पर ही चलता है। कोई फ़ोटो बाहर नहीं जाती।"),

        ["disclaimer"] = ("This is not a diagnosis. Only a blood test can tell whether you have anaemia.",
                          "ഇതൊരു രോഗനിർണ്ണയമല്ല. വിളർച്ച ഉണ്ടോ എന്ന് രക്തപരിശോധനയിലൂടെ മാത്രമേ അറിയാൻ കഴിയൂ.",
                          "यह निदान नहीं है। एनीमिया है या नहीं, यह केवल खून की जाँच से पता चलता है।"),

        ["check.title"] = ("Eyelid check", "കൺപോള പരിശോധന", "पलक जाँच"),
        ["step.prepare"] = ("Prepare", "തയ്യാറാകുക", "तैयारी"),
        ["step.photo"] = ("Photo", "ഫോട്ടോ", "फ़ोटो"),
        ["step.card"] = ("Card", "കാർഡ്", "कार्ड"),
        ["step.paint"] = ("Eyelid", "കൺപോള", "पलक"),
        ["step.result"] = ("Result", "ഫലം", "परिणाम"),
        ["prep.1"] = ("Stand near a window in daylight. Avoid direct sun and the camera flash.",
                      "പകൽവെളിച്ചത്തിൽ ജനലിനടുത്ത് നിൽക്കുക. നേരിട്ടുള്ള വെയിലും ക്യാമറ ഫ്ലാഷും ഒഴിവാക്കുക.",
                      "दिन की रोशनी में खिड़की के पास खड़े हों। सीधी धूप और कैमरा फ़्लैश से बचें।"),
        ["prep.2"] = ("Hold the white card right next to your eye, in the same light.",
                      "വെള്ള കാർഡ് അതേ വെളിച്ചത്തിൽ കണ്ണിന് തൊട്ടടുത്ത് പിടിക്കുക.",
                      "सफ़ेद कार्ड को उसी रोशनी में आँख के ठीक पास पकड़ें।"),
        ["prep.3"] = ("With a clean finger, gently pull your lower eyelid down and look up.",
                      "വൃത്തിയുള്ള വിരൽ കൊണ്ട് താഴത്തെ കൺപോള പതുക്കെ താഴേക്ക് വലിച്ച് മുകളിലേക്ക് നോക്കുക.",
                      "साफ़ उंगली से निचली पलक को धीरे से नीचे खींचें और ऊपर देखें।"),
        ["prep.4"] = ("Ask someone to take a close, sharp photo showing the red inner eyelid and the card.",
                      "ചുവന്ന ഉൾക്കൺപോളയും കാർഡും കാണുന്ന വിധത്തിൽ അടുത്തുനിന്ന് വ്യക്തമായ ഫോട്ടോ എടുക്കാൻ ഒരാളോട് ആവശ്യപ്പെടുക.",
                      "किसी से कहें कि पास से साफ़ फ़ोटो ले, जिसमें लाल अंदरूनी पलक और कार्ड दोनों दिखें।"),
        ["prep.nocard"] = ("No printer? Plain white paper works almost as well.", "പ്രിന്റർ ഇല്ലേ? സാധാരണ വെള്ള പേപ്പറും മതി.", "प्रिंटर नहीं है? सादा सफ़ेद कागज़ भी लगभग उतना ही अच्छा है।"),
        ["prep.next"] = ("I'm ready", "ഞാൻ തയ്യാർ", "मैं तैयार हूँ"),
        ["photo.take"] = ("Take photo", "ഫോട്ടോ എടുക്കുക", "फ़ोटो लें"),
        ["photo.choose"] = ("Choose from gallery", "ഗാലറിയിൽ നിന്ന് തിരഞ്ഞെടുക്കുക", "गैलरी से चुनें"),
        ["photo.sample"] = ("Try a research sample", "ഒരു ഗവേഷണ സാമ്പിൾ പരീക്ഷിക്കുക", "शोध का नमूना आज़माएँ"),
        ["photo.sampleNote"] = ("Samples are real eyelids from the research dataset that the model never saw. They are already outlined and have no card.",
                                "മോഡൽ ഒരിക്കലും കണ്ടിട്ടില്ലാത്ത ഗവേഷണ ഡാറ്റാസെറ്റിലെ യഥാർത്ഥ കൺപോളകളാണ് സാമ്പിളുകൾ. അവ ഇതിനകം അടയാളപ്പെടുത്തിയവയാണ്, കാർഡ് ഇല്ല.",
                                "नमूने शोध डेटासेट की असली पलकें हैं जिन्हें मॉडल ने कभी नहीं देखा। वे पहले से चिह्नित हैं और उनमें कार्ड नहीं है।"),
        ["card.tap"] = ("Tap the middle of the white card in your photo.", "ഫോട്ടോയിലെ വെള്ള കാർഡിന്റെ നടുവിൽ തൊടുക.", "फ़ोटो में सफ़ेद कार्ड के बीच में टैप करें।"),
        ["card.picked"] = ("Card colour captured", "കാർഡിന്റെ നിറം രേഖപ്പെടുത്തി", "कार्ड का रंग दर्ज हुआ"),
        ["card.uneven"] = ("That spot isn't evenly white. Tap a plain white area of the card.", "ആ ഭാഗം ഒരുപോലെ വെള്ളയല്ല. കാർഡിന്റെ വെള്ള ഭാഗത്ത് തൊടുക.", "वह जगह एक जैसी सफ़ेद नहीं है। कार्ड के सादे सफ़ेद हिस्से पर टैप करें।"),
        ["card.skip"] = ("No card in photo — skip (less accurate)", "ഫോട്ടോയിൽ കാർഡ് ഇല്ല — ഒഴിവാക്കുക (കൃത്യത കുറയും)", "फ़ोटो में कार्ड नहीं — छोड़ें (कम सटीक)"),
        ["paint.howto"] = ("Paint over the red inner eyelid only. Avoid lashes, skin and shiny spots. Zoom in for precision.",
                           "ഉൾക്കൺപോളയുടെ ചുവന്ന ഭാഗത്ത് മാത്രം നിറം നൽകുക. കൺപീലികൾ, തൊലി, തിളങ്ങുന്ന പാടുകൾ എന്നിവ ഒഴിവാക്കുക. കൃത്യതയ്ക്കായി സൂം ചെയ്യുക.",
                           "केवल पलक के अंदर के लाल हिस्से पर रंग भरें। पलकों के बाल, त्वचा और चमकदार धब्बों से बचें। सटीकता के लिए ज़ूम करें।"),
        ["paint.measure"] = ("Measure", "അളക്കുക", "मापें"),
        ["tool.wand"] = ("Smart select", "സ്മാർട്ട് സെലക്ട്", "स्मार्ट चयन"),
        ["tool.tolerance"] = ("Spread", "വ്യാപ്തി", "फैलाव"),
        ["paint.wand"] = ("Tap the red inner eyelid — Smart select picks it out. Fix the edges with Paint or Erase.",
                          "ചുവന്ന ഉൾക്കൺപോളയിൽ തൊടുക — സ്മാർട്ട് സെലക്ട് അത് തിരഞ്ഞെടുക്കും. അരികുകൾ നിറം, മായ്ക്കുക എന്നിവ ഉപയോഗിച്ച് ശരിയാക്കുക.",
                          "लाल अंदरूनी पलक पर टैप करें — स्मार्ट चयन उसे चुन लेगा। किनारों को रंगें या मिटाएँ से ठीक करें।"),
        ["wand.tooLarge"] = ("That spread too far. Lower Spread, or zoom in and tap again.", "അത് വളരെ ദൂരം പടർന്നു. വ്യാപ്തി കുറയ്ക്കുക, അല്ലെങ്കിൽ സൂം ചെയ്ത് വീണ്ടും തൊടുക.", "यह बहुत दूर तक फैल गया। फैलाव कम करें, या ज़ूम करके फिर टैप करें।"),
        ["wand.nothing"] = ("Nothing selected there. Tap right on the red inner eyelid.", "അവിടെ ഒന്നും തിരഞ്ഞെടുത്തില്ല. ചുവന്ന ഉൾക്കൺപോളയിൽ തന്നെ തൊടുക.", "वहाँ कुछ नहीं चुना गया। सीधे लाल अंदरूनी पलक पर टैप करें।"),
        ["card.auto"] = ("Card found automatically", "കാർഡ് സ്വയം കണ്ടെത്തി", "कार्ड अपने-आप मिल गया"),
        ["card.change"] = ("Change", "മാറ്റുക", "बदलें"),
        ["card.notFound"] = ("Couldn't find the card automatically. Tap the middle of the white square.", "കാർഡ് സ്വയം കണ്ടെത്താനായില്ല. വെള്ള ചതുരത്തിന്റെ നടുവിൽ തൊടുക.", "कार्ड अपने-आप नहीं मिला। सफ़ेद वर्ग के बीच में टैप करें।"),
        ["result.basedOn"] = ("Average of {0} photos (range {1}–{2})", "{0} ഫോട്ടോകളുടെ ശരാശരി (പരിധി {1}–{2})", "{0} फ़ोटो का औसत (सीमा {1}–{2})"),
        ["result.addPhoto"] = ("Add another photo (steadier reading)", "ഒരു ഫോട്ടോ കൂടി ചേർക്കുക (കൂടുതൽ സ്ഥിരതയുള്ള ഫലം)", "एक और फ़ोटो जोड़ें (ज़्यादा स्थिर परिणाम)"),
        ["result.addPhotoWhy"] = ("Every photo is slightly different; the average of 3 is steadier than one.", "ഓരോ ഫോട്ടോയും അൽപം വ്യത്യസ്തമാണ്; ഒന്നിനേക്കാൾ 3 എണ്ണത്തിന്റെ ശരാശരി കൂടുതൽ സ്ഥിരതയുള്ളതാണ്.", "हर फ़ोटो थोड़ी अलग होती है; एक से ज़्यादा स्थिर 3 का औसत होता है।"),
        ["symptoms.title"] = ("Do any of these apply to you?", "ഇവയിൽ ഏതെങ്കിലും നിങ്ങൾക്ക് ബാധകമാണോ?", "क्या इनमें से कोई बात आप पर लागू होती है?"),
        ["symptoms.tired"] = ("Often tired or weak", "പലപ്പോഴും ക്ഷീണമോ തളർച്ചയോ", "अक्सर थकान या कमज़ोरी"),
        ["symptoms.breath"] = ("Breathless or heart racing when climbing stairs", "പടി കയറുമ്പോൾ ശ്വാസംമുട്ടലോ നെഞ്ചിടിപ്പോ", "सीढ़ियाँ चढ़ते समय साँस फूलना या दिल तेज़ धड़कना"),
        ["symptoms.dizzy"] = ("Dizziness or frequent headaches", "തലകറക്കമോ ഇടയ്ക്കിടെ തലവേദനയോ", "चक्कर आना या बार-बार सिरदर्द"),
        ["symptoms.periods"] = ("Heavy periods, or periods longer than 7 days", "അമിതമായ ആർത്തവം, അല്ലെങ്കിൽ 7 ദിവസത്തിൽ കൂടുതൽ നീളുന്ന ആർത്തവം", "भारी माहवारी, या 7 दिन से ज़्यादा चलने वाली माहवारी"),
        ["symptoms.nails"] = ("Pale palms or nails, or brittle nails", "വിളറിയ ഉള്ളംകൈയോ നഖങ്ങളോ, അല്ലെങ്കിൽ പൊട്ടുന്ന നഖങ്ങൾ", "पीली हथेलियाँ या नाखून, या टूटने वाले नाखून"),
        ["symptoms.pica"] = ("Craving ice, clay, chalk or raw rice", "ഐസ്, മണ്ണ്, ചോക്ക്, പച്ചരി എന്നിവ കഴിക്കാനുള്ള കൊതി", "बर्फ़, मिट्टी, चॉक या कच्चे चावल खाने की तलब"),
        ["symptoms.any"] = ("That alone is a good reason to get a haemoglobin test, whatever the photo says.", "ഫോട്ടോ എന്ത് പറഞ്ഞാലും, ഇതുതന്നെ ഹീമോഗ്ലോബിൻ പരിശോധന നടത്താൻ മതിയായ കാരണമാണ്.", "फ़ोटो चाहे जो कहे, सिर्फ़ यही हीमोग्लोबिन जाँच कराने का अच्छा कारण है।"),
        ["home.tryNow"] = ("Try it in one tap", "ഒറ്റ ടാപ്പിൽ പരീക്ഷിക്കുക", "एक टैप में आज़माएँ"),
        ["tool.move"] = ("Move", "നീക്കുക", "हिलाएँ"),
        ["tool.card"] = ("Card", "കാർഡ്", "कार्ड"),
        ["tool.paint"] = ("Paint", "നിറം", "रंगें"),
        ["tool.erase"] = ("Erase", "മായ്ക്കുക", "मिटाएँ"),
        ["tool.clear"] = ("Clear", "എല്ലാം മായ്ക്കുക", "सब साफ़ करें"),
        ["tool.brush"] = ("Brush", "ബ്രഷ്", "ब्रश"),
        ["tool.zoom"] = ("Zoom", "സൂം", "ज़ूम"),
        ["back"] = ("Back", "പിന്നോട്ട്", "वापस"),
        ["next"] = ("Next", "അടുത്തത്", "आगे"),
        ["restart"] = ("New check", "പുതിയ പരിശോധന", "नई जाँच"),

        ["band.Lower"] = ("Less pale", "വിളറൽ കുറവ്", "पीलापन कम"),
        ["band.InBetween"] = ("In the overlap zone", "ഇടകലർന്ന മേഖലയിൽ", "बीच के क्षेत्र में"),
        ["band.Higher"] = ("Paler than usual", "സാധാരണയേക്കാൾ വിളറിയത്", "सामान्य से ज़्यादा पीला"),
        ["advice.Lower"] = ("Your eyelid looks about as red as most healthy eyes in the reference set. This does not rule out anaemia: if you feel tired, breathless or dizzy, get a blood test.",
                            "റഫറൻസിലെ മിക്ക ആരോഗ്യമുള്ള കണ്ണുകളെയും പോലെ നിങ്ങളുടെ കൺപോള ചുവന്നതായി കാണുന്നു. ഇത് വിളർച്ച ഇല്ലെന്ന് ഉറപ്പാക്കുന്നില്ല: ക്ഷീണം, ശ്വാസംമുട്ട്, തലകറക്കം എന്നിവ ഉണ്ടെങ്കിൽ രക്തപരിശോധന നടത്തുക.",
                            "आपकी पलक संदर्भ सेट की ज़्यादातर स्वस्थ आँखों जितनी लाल दिखती है। इससे एनीमिया रद्द नहीं होता: अगर थकान, साँस फूलना या चक्कर आए, तो खून की जाँच कराएँ।"),
        ["advice.InBetween"] = ("Your result is where healthy and anaemic eyes look alike. A photo cannot decide this — a blood test can.",
                                "ആരോഗ്യമുള്ളതും വിളർച്ചയുള്ളതുമായ കണ്ണുകൾ ഒരുപോലെ കാണുന്ന മേഖലയിലാണ് നിങ്ങളുടെ ഫലം. ഇത് ഫോട്ടോയ്ക്ക് തീരുമാനിക്കാനാവില്ല — രക്തപരിശോധനയ്ക്ക് കഴിയും.",
                                "आपका परिणाम उस क्षेत्र में है जहाँ स्वस्थ और एनीमिया वाली आँखें एक जैसी दिखती हैं। फ़ोटो यह तय नहीं कर सकती — खून की जाँच कर सकती है।"),
        ["advice.Higher"] = ("Your eyelid looks paler than most healthy eyes in the reference set. Please get a haemoglobin blood test soon.",
                             "റഫറൻസിലെ മിക്ക ആരോഗ്യമുള്ള കണ്ണുകളേക്കാളും നിങ്ങളുടെ കൺപോള വിളറിയതായി കാണുന്നു. എത്രയും വേഗം ഹീമോഗ്ലോബിൻ രക്തപരിശോധന നടത്തുക.",
                             "आपकी पलक संदर्भ सेट की ज़्यादातर स्वस्थ आँखों से ज़्यादा पीली दिखती है। कृपया जल्द ही हीमोग्लोबिन की खून की जाँच कराएँ।"),
        ["result.index"] = ("Pallor index", "വിളറൽ സൂചിക", "पीलापन सूचकांक"),
        ["result.chart"] = ("Where your eyelid sits among {0} reference eyes", "{0} റഫറൻസ് കണ്ണുകൾക്കിടയിൽ നിങ്ങളുടെ കൺപോളയുടെ സ്ഥാനം", "{0} संदर्भ आँखों के बीच आपकी पलक कहाँ है"),
        ["result.healthy"] = ("Healthy (Hb ≥ 11)", "ആരോഗ്യമുള്ളത് (Hb ≥ 11)", "स्वस्थ (Hb ≥ 11)"),
        ["result.anaemic"] = ("Anaemic (Hb < 11)", "വിളർച്ചയുള്ളത് (Hb < 11)", "एनीमिया (Hb < 11)"),
        ["result.you"] = ("You", "നിങ്ങൾ", "आप"),
        ["result.honest"] = ("How good is this? On {0} eyes it had never seen, PaleCheck scored AUC {1} (1.0 is perfect, 0.5 is a coin flip). Treat it as a nudge, not an answer.",
                             "ഇത് എത്ര കൃത്യമാണ്? മുമ്പ് കണ്ടിട്ടില്ലാത്ത {0} കണ്ണുകളിൽ PaleCheck-ന്റെ AUC {1} ആയിരുന്നു (1.0 പൂർണ്ണം, 0.5 നാണയം എറിയുന്നതിന് തുല്യം). ഇതൊരു സൂചന മാത്രമാണ്, ഉത്തരമല്ല.",
                             "यह कितना सटीक है? {0} ऐसी आँखों पर जिन्हें इसने पहले नहीं देखा, PaleCheck का AUC {1} रहा (1.0 पूर्ण है, 0.5 सिक्का उछालने जैसा)। इसे संकेत समझें, उत्तर नहीं।"),
        ["result.reference"] = ("Reference eyes are young children from Ghana; teenage and adult eyes may differ.",
                                "റഫറൻസ് കണ്ണുകൾ ഘാനയിലെ ചെറിയ കുട്ടികളുടേതാണ്; കൗമാരക്കാരുടെയും മുതിർന്നവരുടെയും കണ്ണുകൾ വ്യത്യസ്തമാകാം.",
                                "संदर्भ आँखें घाना के छोटे बच्चों की हैं; किशोरों और वयस्कों की आँखें अलग हो सकती हैं।"),
        ["result.sampleTruth"] = ("Lab result for this sample: Hb {0} g/dL — {1}.", "ഈ സാമ്പിളിന്റെ ലാബ് ഫലം: Hb {0} g/dL — {1}.", "इस नमूने का लैब परिणाम: Hb {0} g/dL — {1}।"),
        ["word.anaemic"] = ("anaemic", "വിളർച്ചയുണ്ട്", "एनीमिया है"),
        ["word.healthy"] = ("not anaemic", "വിളർച്ചയില്ല", "एनीमिया नहीं है"),
        ["result.save"] = ("Save to my history (this phone only)", "എന്റെ ചരിത്രത്തിൽ സൂക്ഷിക്കുക (ഈ ഫോണിൽ മാത്രം)", "मेरे इतिहास में सहेजें (केवल इस फ़ोन पर)"),
        ["result.saved"] = ("Saved on this phone.", "ഈ ഫോണിൽ സൂക്ഷിച്ചു.", "इस फ़ोन पर सहेजा गया।"),
        ["result.notMeasured"] = ("We couldn't measure this photo", "ഈ ഫോട്ടോ അളക്കാൻ കഴിഞ്ഞില്ല", "यह फ़ोटो मापी नहीं जा सकी"),
        ["test.title"] = ("Get a free blood test", "സൗജന്യ രക്തപരിശോധന നടത്തുക", "मुफ़्त खून की जाँच कराएँ"),
        ["test.body"] = ("Under Anaemia Mukt Bharat, school students aged 10–19 get yearly haemoglobin screening and weekly iron-folic acid (blue) tablets. Ask your school, ASHA worker or nearest government health centre.",
                         "അനീമിയ മുക്ത് ഭാരത് പദ്ധതി പ്രകാരം 10–19 വയസ്സുള്ള സ്കൂൾ വിദ്യാർത്ഥികൾക്ക് വർഷംതോറും ഹീമോഗ്ലോബിൻ പരിശോധനയും ആഴ്ചതോറും അയൺ-ഫോളിക് ആസിഡ് (നീല) ഗുളികകളും ലഭിക്കും. നിങ്ങളുടെ സ്കൂളിനോടോ ആശാ പ്രവർത്തകയോടോ അടുത്തുള്ള സർക്കാർ ആരോഗ്യ കേന്ദ്രത്തോടോ ചോദിക്കുക.",
                         "एनीमिया मुक्त भारत के तहत 10–19 वर्ष के स्कूली विद्यार्थियों को हर साल हीमोग्लोबिन जाँच और हर हफ़्ते आयरन-फ़ोलिक एसिड (नीली) गोलियाँ मिलती हैं। अपने स्कूल, आशा कार्यकर्ता या नज़दीकी सरकारी स्वास्थ्य केंद्र से पूछें।"),
        ["food.title"] = ("Foods that help", "സഹായിക്കുന്ന ഭക്ഷണങ്ങൾ", "मददगार भोजन"),
        ["food.body"] = ("Iron-rich: drumstick (moringa) leaves, spinach and other greens, ragi, horse gram, chickpeas, jaggery, dates, eggs, fish and meat. Eat them with vitamin C (amla, guava, lemon) and avoid tea or coffee within an hour of meals.",
                         "അയൺ കൂടുതലുള്ളവ: മുരിങ്ങയില, ചീര ഉൾപ്പെടെയുള്ള ഇലക്കറികൾ, റാഗി (മുത്താറി), മുതിര, കടല, ശർക്കര, ഈന്തപ്പഴം, മുട്ട, മീൻ, ഇറച്ചി. ഇവ വിറ്റാമിൻ C (നെല്ലിക്ക, പേരക്ക, നാരങ്ങ) യോടൊപ്പം കഴിക്കുക; ഭക്ഷണത്തിന് ഒരു മണിക്കൂറിനുള്ളിൽ ചായയും കാപ്പിയും ഒഴിവാക്കുക.",
                         "आयरन से भरपूर: सहजन (मोरिंगा) के पत्ते, पालक और अन्य हरी सब्ज़ियाँ, रागी, कुलथी, चना, गुड़, खजूर, अंडे, मछली और मांस। इन्हें विटामिन C (आँवला, अमरूद, नींबू) के साथ खाएँ और भोजन के एक घंटे के भीतर चाय या कॉफ़ी से बचें।"),

        ["q.OutlineTooSmall"] = ("Paint a larger area of the inner eyelid (zoom in and paint more).", "ഉൾക്കൺപോളയുടെ കൂടുതൽ ഭാഗം നിറം നൽകുക (സൂം ചെയ്ത് കൂടുതൽ നിറം നൽകുക).", "पलक के अंदरूनी हिस्से का बड़ा भाग रंगें (ज़ूम करके और रंगें)।"),
        ["q.TooMuchGlare"] = ("Too many shiny spots. Avoid flash and direct light, then retake.", "തിളങ്ങുന്ന പാടുകൾ കൂടുതലാണ്. ഫ്ലാഷും നേരിട്ടുള്ള വെളിച്ചവും ഒഴിവാക്കി വീണ്ടും എടുക്കുക.", "बहुत ज़्यादा चमकदार धब्बे हैं। फ़्लैश और सीधी रोशनी से बचें, फिर दोबारा फ़ोटो लें।"),
        ["q.TooDark"] = ("Too dark. Move closer to a window and retake.", "വെളിച്ചം വളരെ കുറവാണ്. ജനലിനടുത്തേക്ക് നീങ്ങി വീണ്ടും എടുക്കുക.", "बहुत अँधेरा है। खिड़की के पास जाकर दोबारा फ़ोटो लें।"),
        ["q.NoCard"] = ("No card: room lighting can change this result a lot.", "കാർഡ് ഇല്ല: മുറിയിലെ വെളിച്ചം ഈ ഫലത്തെ വളരെയധികം മാറ്റാം.", "कार्ड नहीं: कमरे की रोशनी इस परिणाम को काफ़ी बदल सकती है।"),
        ["q.CardOverexposed"] = ("The card is washed out. Tilt it slightly away from the light.", "കാർഡ് വളരെ തെളിഞ്ഞുപോയി. വെളിച്ചത്തിൽ നിന്ന് അൽപം ചരിച്ചു പിടിക്കുക.", "कार्ड बहुत ज़्यादा चमकीला है। इसे रोशनी से थोड़ा हटाकर पकड़ें।"),
        ["q.CardTooDark"] = ("The card is too dark. Use more light.", "കാർഡ് വളരെ ഇരുണ്ടതാണ്. കൂടുതൽ വെളിച്ചം ഉപയോഗിക്കുക.", "कार्ड बहुत गहरा है। ज़्यादा रोशनी का इस्तेमाल करें।"),
        ["q.StrongColouredLight"] = ("Very coloured light (like a yellow bulb). The card corrects it, but daylight is better.", "വളരെ നിറമുള്ള വെളിച്ചം (മഞ്ഞ ബൾബ് പോലെ). കാർഡ് ഇത് ശരിയാക്കും, പക്ഷേ പകൽവെളിച്ചമാണ് നല്ലത്.", "बहुत रंगीन रोशनी (जैसे पीला बल्ब)। कार्ड इसे ठीक करता है, लेकिन दिन की रोशनी बेहतर है।"),

        ["cardpage.title"] = ("The ₹1 calibration card", "₹1 കാലിബ്രേഷൻ കാർഡ്", "₹1 कैलिब्रेशन कार्ड"),
        ["cardpage.body"] = ("Print this page on plain white paper and cut out a card. In every photo, hold it next to your eye so it gets the same light. PaleCheck uses its white square to cancel the colour of the room light.",
                             "ഈ പേജ് വെള്ള പേപ്പറിൽ പ്രിന്റ് ചെയ്ത് ഒരു കാർഡ് മുറിച്ചെടുക്കുക. ഓരോ ഫോട്ടോയിലും അതേ വെളിച്ചം കിട്ടുന്നതിനായി അത് കണ്ണിനടുത്ത് പിടിക്കുക. മുറിയിലെ വെളിച്ചത്തിന്റെ നിറം ഇല്ലാതാക്കാൻ PaleCheck അതിലെ വെള്ള ചതുരം ഉപയോഗിക്കുന്നു.",
                             "इस पेज को सादे सफ़ेद कागज़ पर छापें और एक कार्ड काट लें। हर फ़ोटो में इसे आँख के पास रखें ताकि उस पर वही रोशनी पड़े। PaleCheck इसके सफ़ेद वर्ग से कमरे की रोशनी का रंग हटाता है।"),
        ["cardpage.print"] = ("Print cards", "കാർഡുകൾ പ്രിന്റ് ചെയ്യുക", "कार्ड छापें"),

        ["history.title"] = ("My history", "എന്റെ ചരിത്രം", "मेरा इतिहास"),
        ["history.empty"] = ("No saved checks yet.", "ഇതുവരെ സൂക്ഷിച്ച പരിശോധനകളൊന്നുമില്ല.", "अभी तक कोई जाँच सहेजी नहीं गई।"),
        ["history.note"] = ("Stored only in this browser. Comparing your own results over time (same card, same window) is more meaningful than one reading.",
                            "ഈ ബ്രൗസറിൽ മാത്രം സൂക്ഷിക്കുന്നു. ഒരൊറ്റ ഫലത്തേക്കാൾ, കാലക്രമേണ നിങ്ങളുടെ തന്നെ ഫലങ്ങൾ (അതേ കാർഡ്, അതേ ജനൽ) താരതമ്യം ചെയ്യുന്നതാണ് കൂടുതൽ അർത്ഥവത്ത്.",
                            "केवल इसी ब्राउज़र में सहेजा गया। एक बार के परिणाम से ज़्यादा, समय के साथ अपने परिणामों की तुलना (वही कार्ड, वही खिड़की) ज़्यादा सार्थक है।"),
        ["history.export"] = ("Export CSV", "CSV ആയി എടുക്കുക", "CSV निर्यात करें"),
        ["history.clear"] = ("Delete all", "എല്ലാം ഇല്ലാതാക്കുക", "सब हटाएँ"),
        ["history.labHb"] = ("Lab Hb (g/dL)", "ലാബ് Hb (g/dL)", "लैब Hb (g/dL)"),
    };
}

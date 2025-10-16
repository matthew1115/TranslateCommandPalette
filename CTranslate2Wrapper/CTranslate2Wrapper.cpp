#include "pch.h"
#include "CTranslate2Wrapper.h"

// Required C++ standard library headers
#include <vector>
#include <string>
#include <memory>

// CTranslate2, sentencepiece and C++/CLI interop headers
#include <ctranslate2/translator.h>
#include <sentencepiece_processor.h>

// Add these includes for UTF-8/UTF-16 conversion
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>
#include <codecvt>

std::string toUtf8(System::String^ s) {
	using namespace System::Runtime::InteropServices;
	IntPtr ptr = Marshal::StringToHGlobalUni(s);
	const wchar_t* wstr = static_cast<const wchar_t*>(ptr.ToPointer());

	int len = WideCharToMultiByte(CP_UTF8, 0, wstr, -1, nullptr, 0, nullptr, nullptr);
	std::string utf8(len - 1, 0);
	WideCharToMultiByte(CP_UTF8, 0, wstr, -1, utf8.data(), len, nullptr, nullptr);

	Marshal::FreeHGlobal(ptr);
	return utf8;
}

System::String^ fromUtf8(const std::string& s) {
	int len = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), -1, nullptr, 0);
	std::wstring wstr(len - 1, 0);
	MultiByteToWideChar(CP_UTF8, 0, s.c_str(), -1, wstr.data(), len);
	return gcnew System::String(wstr.c_str());
}

// This is the Private Implementation (PImpl) idiom.
// It hides the native C++ types from the header file, which improves compile times
// and prevents issues with including native headers in C# projects.
class CTranslate2WrapperImpl
{
public:
	std::unique_ptr<std::string> nativeModelPath;
	// This holds the pointer to the actual CTranslate2 engine.
	std::unique_ptr<ctranslate2::Translator> translator;
};

// Use the namespace defined in your header file
using namespace CTranslate2Wrapper;

// Constructor: Initializes the native translator engine.
Translator::Translator(String^ modelPath)
{
	m_pImpl = new CTranslate2WrapperImpl();
	try
	{
		// Convert the managed .NET string to a native C++ std::string.
		m_pImpl->nativeModelPath = std::make_unique<std::string>(toUtf8(modelPath));

		// Create the native CTranslate2 Translator object.
		const std::vector<int> device_indices = { 0 };
		m_pImpl->translator = std::make_unique<ctranslate2::Translator>(*(m_pImpl->nativeModelPath), ctranslate2::Device::CPU, ctranslate2::ComputeType::INT8, device_indices);
	}
	catch (const std::exception& e)
	{
		// If the native code throws an exception (e.g., model not found),
		// clean up and re-throw it as a managed exception that C# can catch.
		delete m_pImpl;
		m_pImpl = nullptr;
		throw gcnew Exception(msclr::interop::marshal_as<String^>(e.what()));
	}
}

// Translate Method: This is the core function your C# app will call.
String^ Translator::Translate(String^ text)
{
	if (m_pImpl == nullptr)
	{
		throw gcnew ObjectDisposedException("Translator instance has been disposed.");
	}

	// Load the SentencePiece spm file.
	sentencepiece::SentencePieceProcessor tokenizer;
	std::string sp_model_path = *(m_pImpl->nativeModelPath) + "/source.spm";
	std::string debugMsg = "Trying to load SentencePiece model from: " + sp_model_path + "\n";
	OutputDebugStringA(debugMsg.c_str());
	const auto sp_status = tokenizer.Load(sp_model_path);
	if (!sp_status.ok())
	{
		throw std::runtime_error("Failed to load SentencePiece model: " + sp_status.ToString());
	}

	// 1. Marshal (convert) the input .NET string to a native C++ string.
	std::string nativeText = toUtf8(text);

	// 2. Tokenize the input string. Use sentencepiece
	std::vector<std::string> tokens;
	tokenizer.Encode(nativeText, &tokens);
	// Add BOS and EOS tokens
	//tokens.insert(tokens.begin(), "<s>");
	// opusmt does not need BOS tokens
	tokens.push_back("</s>");

	// 3. The translate_batch method expects a vector of sentences.
	//    We wrap our single sentence's tokens in another vector to create a batch of one.
	std::vector<std::vector<std::string>> batch_tokens = { tokens };

	// Translation options
	ctranslate2::TranslationOptions options;
	options.beam_size = 2;
	options.num_hypotheses = 1;
	options.max_decoding_length = 256;
	options.return_scores = false;
	// additional options
	options.repetition_penalty = 1.1;

	// 4. Call the CTranslate2 engine.
	const std::vector<ctranslate2::TranslationResult> results = m_pImpl->translator->translate_batch(batch_tokens, options);

	if (results.empty() || results[0].hypotheses.empty())
	{
		return String::Empty;
	}

	// Remove BOS/EOS tokens
	auto hypothesis = results[0].hypotheses[0];
	if (!hypothesis.empty() && hypothesis.front() == "<s>")
		hypothesis.erase(hypothesis.begin());
	if (!hypothesis.empty() && hypothesis.back() == "</s>")
		hypothesis.pop_back();

	std::string translatedText;
	auto status = tokenizer.Decode(hypothesis, &translatedText);
	if (!status.ok())
	{
		throw gcnew Exception(msclr::interop::marshal_as<String^>(
			"Failed to decode SentencePiece tokens: " + status.ToString()));
	}

	// 6. Marshal the native C++ string result back to a .NET string and return it.
	return fromUtf8(translatedText);
}

// This is the IDisposable pattern for C++/CLI.
// The destructor (~), called by C#'s 'using' block, chains to the finalizer (!).
Translator::~Translator()
{
	this->!Translator();
}

// The finalizer is the last line of defense to clean up unmanaged resources.
Translator::!Translator()
{
	if (m_pImpl != nullptr)
	{
		delete m_pImpl;
		m_pImpl = nullptr;
	}
}
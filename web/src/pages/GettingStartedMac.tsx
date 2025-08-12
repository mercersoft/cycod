import TerminalCardMac from "@/components/TerminalCardMac"

export default function GettingStartedMac() {
  return (
    <>
      <h1 className="text-3xl md:text-4xl font-mono text-green-400">Getting Started</h1>
      <p className="text-gray-300 mt-4">
        Installing the CycoDev CLI on your Mac is easy. Just run the following command in your terminal:
      </p>

      <div className="mt-8">
        <TerminalCardMac
          title="Terminal — macOS"
          commands={[
            {
              command: "brew tap robch/cycod",
              output: [],
            },
            {
              command: "brew install cycod",
              output: [
                "installing cycod",
                "",
                "Building cycod",
                "",
                "Installing cycod",
                "",
                "cycod is installed!",
                "",
                "cleaning up build files",
                "",
                "Run 'cycod --help' for more details.",
              ],
            },
          ]}
        />
      </div>
    </>
  )
}



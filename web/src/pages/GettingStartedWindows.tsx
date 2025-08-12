import TerminalCardPS from "@/components/TerminalCardPS"

export default function GettingStartedWindows() {
  return (
    <>
      <h1 className="text-3xl md:text-4xl font-mono text-green-400">Getting Started</h1>
      <p className="text-gray-300 mt-4">
        Installing the CycoDev CLI on Windows using PowerShell is easy. Run the following commands:
      </p>

      <div className="mt-8">
        <TerminalCardPS
          title="Windows PowerShell"
          commands={[
            {
              command: "winget install --id Git.Git -e",
              output: [
                "Found Git [Git.Git]",
                "This application is licensed to you by its owner.",
                "Successfully installed",
              ],
            },
            {
              command: "dotnet tool install --global cycodmd --prerelease",
              output: [
                "You can invoke the tool using the following command: cycodmd",
                "Tool 'cycodmd' (version '1.x.x') was successfully installed.",
              ],
            },
          ]}
        />
      </div>
    </>
  )
}



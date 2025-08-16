import { useWebSocket } from "@/context/WebSocketContext"

export default function DeamonStatus() {
  const { status: state, handleButtonClick } = useWebSocket()

  const getStatusColor = () => {
    switch (state.status) {
      case 'connected':
        return 'text-yellow-400'
      case 'started':
        return 'text-green-400'
      case 'error':
        return 'text-red-400'
      default:
        return 'text-gray-300'
    }
  }

  const getButtonText = () => {
    if (state.status === 'started') {
      return 'Stop'
    } else if (state.status === 'connected') {
      return 'Start'
    } else if (state.status === 'checking') {
      return 'Connecting...'
    } else {
      return 'Refresh'
    }
  }

  const getButtonStyle = () => {
    const baseStyle = "px-3 py-1 text-sm rounded border transition-colors duration-200"
    
    if (state.status === 'started') {
      return `${baseStyle} border-red-500 text-red-400 hover:bg-red-500 hover:text-white`
    } else if (state.status === 'connected') {
      return `${baseStyle} border-green-500 text-green-400 hover:bg-green-500 hover:text-white`
    } else if (state.status === 'checking') {
      return `${baseStyle} border-gray-500 text-gray-400 opacity-50 cursor-not-allowed`
    } else {
      return `${baseStyle} border-blue-500 text-blue-400 hover:bg-blue-500 hover:text-white`
    }
  }

  return (
    <div className="flex items-center gap-3 text-white">
      <button
        onClick={handleButtonClick}
        disabled={state.status === 'checking'}
        className={getButtonStyle()}
      >
        {getButtonText()}
      </button>
      <span className="font-medium">Status:</span>
      <span className={getStatusColor()}>
        {state.message}
      </span>
    </div>
  )
}